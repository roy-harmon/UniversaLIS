using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Collections;
using System.Data.Common;
using System.Text.Json;
using System.Text.Json.Serialization;
using UniversaLIS.Models;

namespace REST_LIS
{
     public class Program
     {
          public static void Main(string[] args)
          {
               var builder = WebApplication.CreateBuilder(args);

               // Add services to the container.
               builder.Services.AddAuthorization();

               // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
               builder.Services.AddEndpointsApiExplorer();
               builder.Services.AddSwaggerGen(options =>
               {
                    options.SwaggerDoc("v1", new OpenApiInfo
                    {
                         Version = "v1",
                         Title = "REST-LIS",
                         Description = "An ASP.NET Core Web API for ordering tests and reporting results through UniversaLIS",
                         Contact = new OpenApiContact
                         {
                              Name = "Roy Harmon",
                              Url = new Uri("https://www.linkedin.com/in/roy-r-harmon/")
                         },
                         License = new OpenApiLicense
                         {
                              Name = "MIT License",
                              Url = new Uri("https://github.com/roy-harmon/UniversaLIS/blob/main/LICENSE")
                         }
                    });
                    options.EnableAnnotations();
               });
               builder.Services.AddDbContext<PatientDB>();
               builder.Services.ConfigureAll<JsonSerializerOptions>(opts => {
                    opts.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
                    opts.ReferenceHandler = ReferenceHandler.IgnoreCycles; });

               var app = builder.Build();

               // Configure the HTTP request pipeline.
               if (app.Environment.IsDevelopment())
               {
                    app.UseSwagger();
                    app.UseSwaggerUI();
               }
               
               app.UseHttpsRedirection();
               app.UseAuthorization();
               app.MapGet("/", () => "Hello World!").WithName("HelloWorld").ExcludeFromDescription();

               // Endpoints for records received from testing instrumentation.
               
               // Top level is patients. GET to list them, including child entities (Orders containing test information, Results containing test results).
               // Supports querying by PracticePatientID, PatientName, DOB, and UniversalTestID fields.
               app.MapGet("/reports/patients", ([FromQuery] string? practicePatientID, [FromQuery] string? patientName,
               [FromQuery] string? DOB, [FromQuery] string? universalTestID) =>
               {
                    string pPID = practicePatientID == null ? "%" : practicePatientID;
                    string ppN = patientName == null ? "%" : patientName;
                    string pDOB = DOB == null ? "%" : DOB;
                    string puTID = universalTestID == null ? "^" : universalTestID;
                    using PatientDB dB = new();
                    return dB.GetPatients(pPID, ppN, pDOB, puTID);
               }).WithName("GetPatients").Produces<List<Patient>>(StatusCodes.Status200OK);

               // Get a specific patient record by ID.
               app.MapGet("/reports/patients/{id}", (int id) =>
               {
                    using PatientDB dB = new();
                    return dB.GetPatient(id);
               }).WithName("GetPatientByID").Produces<Patient>(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound);

               // Endpoints for requests sent to the LIS for distribution to testing instrumentation. 

               // Top level is patient requests.
               // POST new requests here to submit them for testing. Include orders.
               app.MapPost("/requests/patients", (PatientRequest patientRequest) =>
               {
                    using PatientDB dB = new();
                    return dB.PostPatientRequest(patientRequest);
               }).WithName("PostPatientOrders").Produces(StatusCodes.Status201Created).Produces(StatusCodes.Status400BadRequest);

               // GET a list of all pending requests.
               app.MapGet("/requests/samples", handler: () =>
               {
                    using PatientDB dB = new();
                    return dB.GetPatientOrders("%", "%");
               }).WithName("QueryPendingRequests").Produces<List<PatientRequest>>(StatusCodes.Status200OK);

               // GET a list of all pending requests for a specific SAMPLE number.
               app.MapGet("/requests/samples/{id}", handler: (string id) =>
               {
                    using PatientDB dB = new();
                    return dB.GetPatientOrders(id, "%");
               }).WithName("QueryPendingRequestsBySampleID").Produces<List<PatientRequest>>(StatusCodes.Status200OK);

               // Set the included orders back to "Pending" status.
               app.MapPost("/requests/pending", handler: ([FromBody] List<PatientRequest> patientRequests) =>
               {
                    using PatientDB db = new();
                    return db.SetPending(patientRequests);
               }).WithName("ResetPendingOrders").Produces(StatusCodes.Status200OK);

               // Add the included results to the database.
               app.MapPost("/reports", handler: ([FromBody] List<Patient> patientResults) =>
               {
                    using PatientDB db = new();
                    return db.AddResults(patientResults);
               }).WithName("PostResultReports").Produces(StatusCodes.Status202Accepted);

               // GET a list of all patient requests. 
               // Supports querying by PracticePatientID, PatientName, DOB, and UniversalTestID fields.
               app.MapGet("/requests/patients", ([FromQuery] string? practicePatientID, [FromQuery] string? patientName,
               [FromQuery] string? DOB, [FromQuery] string? universalTestID) =>
               {
                    string pPID = practicePatientID == null ? "%" : practicePatientID;
                    string ppN = patientName == null ? "%" : patientName;
                    string pDOB = DOB == null ? "%" : DOB;
                    string puTID = universalTestID == null ? "^" : universalTestID;
                    using PatientDB dB = new();
                    return dB.GetPatientRequests(pPID, ppN, pDOB, puTID);
               }).WithName("GetPatientRequests").Produces<List<PatientRequest>>(StatusCodes.Status200OK);

               // Get a specific patient request by ID.
               app.MapGet("/requests/patients/{id}", (int id) =>
               {
                    using PatientDB dB = new();
                    return dB.GetPatientRequestById(id);
               }).WithName("GetPatientOrdersByID").Produces<PatientRequest>(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound);

               // Update a specific patient request by ID.
               app.MapPut("/requests/patients/{id}", (int id,
                    [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Disallow)] PatientRequest patient) =>
               {
                    using PatientDB dB = new();
                    return dB.PutPatientRequestById(id, patient);
               }).WithName("UpdatePatientOrderByID").Produces<PatientRequest>(StatusCodes.Status200OK).Produces(StatusCodes.Status400BadRequest).Produces(StatusCodes.Status404NotFound);

               // Delete a specific patient request by ID.
               app.MapDelete("/requests/patients/{id}", (int id) =>
               {
                    using PatientDB dB = new();
                    return dB.DeletePatientRequestById(id);
               }).WithName("DeletePatientRequestByID").Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound);
               app.Run();
          }
     }

     class PatientDB : DbContext
     {
          private const string LAST_INSERTED = "select last_insert_rowid();";
          public PatientDB() { }

          readonly JsonSerializerOptions jsonOptions = new()
          {
               DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
               ReferenceHandler = ReferenceHandler.IgnoreCycles
          };

          protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
          {
               optionsBuilder.UseSqlite("DataSource=internal.db");
               base.OnConfiguring(optionsBuilder);
          }

          protected override void OnModelCreating(ModelBuilder modelBuilder)
          {
               modelBuilder.Entity<Patient>(p =>
               p.ToTable("PatientRecord").HasKey("PatientID"));
               modelBuilder.Entity<Patient>().Navigation(p => p.Orders).AutoInclude();
               modelBuilder.Entity<Order>(o =>
               o.ToTable("OrderRecord").HasKey("OrderID"));
               modelBuilder.Entity<Order>().Navigation(o => o.Results).AutoInclude();
               modelBuilder.Entity<Result>(r =>
               r.ToTable("ResultRecord").HasKey(r => r.ResultID));
               modelBuilder.Entity<PatientRequest>(pr =>
               pr.ToTable("PatientRequest").HasKey("PatientID"));
               modelBuilder.Entity<PatientRequest>().Navigation(pr => pr.Orders).AutoInclude();
               modelBuilder.Entity<OrderRequest>(or =>
               or.ToTable("OrderRequest").HasKey("OrderID"));
               base.OnModelCreating(modelBuilder);
          }

          public PatientDB(DbContextOptions<PatientDB> options) : base(options) { }
          public DbSet<Patient> PatientReports => Set<Patient>();
          public DbSet<PatientRequest> PatientRequests => Set<PatientRequest>();

          private IResult GetPatientById(int id)
          {
               return Results.Json(PatientReports.Find(id), jsonOptions);
          }

          public IResult GetPatient(int id)
          {
               if (id > 0)
               {
                    return GetPatientById(id);
               }
               return Results.NotFound();
          }

          public IResult GetPatient(HttpContext context)
          {
               int id = int.Parse($"{context.Request.RouteValues["id"]}");
               return GetPatient(id);
          }

          public IResult GetAllPatients()
          {
               return Results.Ok(this.PatientReports.AsNoTracking().ToList());
          }

          public IResult DeletePatientRequestById(int id)
          {
               if (id > 0)
               {
                    PatientRequests.Where(p => p.PatientID.Equals(id)).ExecuteDelete();
                    return Results.NoContent();
               }
               return Results.NotFound();
          }

          public IResult PutPatientRequestById(int id, PatientRequest patient)
          {
               var entity = PatientRequests.Find(id);
               if (entity == null)
               {
                    return Results.NotFound();
               }
               try
               {
                    Entry(entity).CurrentValues.SetValues(patient);
                    SaveChanges();
                    return Results.Ok(entity);
               }
               catch (Exception e)
               {
                    return Results.BadRequest(e.Message);
               }
          }

          public IResult GetPatientRequestById(int id)
          {
               if (id > 0)
               {
                    return Results.Json(PatientRequests.Find(id), jsonOptions);
               }
               return Results.NotFound();
          }

          public IResult PostPatientRequest(PatientRequest patientRequest)
          {
               PatientRequests.Add(patientRequest);
               SaveChanges(true);
               return Results.Json(patientRequest, jsonOptions, "application/json", StatusCodes.Status201Created);
          }

          public IResult GetPatientRequests()
          {
               return Results.Json(PatientRequests.AsNoTracking().ToList(), jsonOptions);
          }

          public IResult GetPatients(string? practicePatientID = "%", string? patientName = "%",
               string? DOB = "%", string? universalTestID = "^")
          {
#pragma warning disable CS8604 // Possible null reference argument.
               return Results.Json(PatientReports.Where(p => EF.Functions.Like(p.PracticePatientID, practicePatientID))
                    .Where(p => EF.Functions.Like(p.PatientName, patientName))
                    .Where(p => EF.Functions.Like(p.DOB, DOB))
                    .AsNoTracking().ToList()
                    .Where(p => p.Orders.Any(o => o.UniversalTestID.Contains(universalTestID))));
#pragma warning restore CS8604 // Possible null reference argument.
          }

          public IResult GetPatientRequests(string? practicePatientID = "%", string? patientName = "%",
               string? DOB = "%", string? universalTestID = "^")
          {
#pragma warning disable CS8604 // Possible null reference argument.
               return Results.Json(PatientRequests.Where(p => EF.Functions.Like(p.PracticePatientID, practicePatientID))
                    .Where(p => EF.Functions.Like(p.PatientName, patientName))
                    .Where(p => EF.Functions.Like(p.DOB, DOB))
                    .AsNoTracking().ToList()
                    .Where(p => p.Orders.Any(o => o.UniversalTestID.Contains(universalTestID))));
#pragma warning restore CS8604 // Possible null reference argument.
          }

          public IResult GetPatientOrders(string SampleNumber, string testID)
          {
               // TODO: Find a way to handle delimited lists of test codes.
               if (testID == "ALL")
               {
                    testID = "%";
               }
               // If no SampleNumber is provided, use the % wildcard to get all pending orders.
               if (SampleNumber.Length == 0)
               {
                    SampleNumber = "%";
               }
               // Query the database for [P]atient and [O]rder records for the sample.
               using DbConnection conn = new SqliteConnection(Database.GetConnectionString());
               conn.Open();
               IList<PatientRequest> patientRequests = new List<PatientRequest>();
               long orderCount;
               using (DbCommand sqlCommand = conn.CreateCommand())
               { // Check to see how many orders are pending for the sample.
                    sqlCommand.CommandText = "SELECT COUNT(OrderRequest.OrderID) AS OrderCount FROM OrderRequest WHERE (SpecimenID LIKE @SampleNumber) AND (UniversalTestID LIKE @TestID) AND (PendingSending = 1);";
                    AddWithValue(sqlCommand, "@SampleNumber", SampleNumber);
                    AddWithValue(sqlCommand, "@TestID", testID);
                    orderCount = (long)(sqlCommand.ExecuteScalar() ?? 0L);
               }

               if (orderCount == 0)
               {    // No pending orders. 
                    return Results.Json(patientRequests);
               }
               string selectPatientsQuery = "SELECT DISTINCT PatientRequest.* FROM PatientRequest JOIN OrderRequest" +
                    " WHERE (SpecimenID LIKE @Sample_Number) AND (UniversalTestID LIKE @Test_ID) AND PendingSending = 1;";
               string selectOrdersQuery = "SELECT * FROM OrderRequest WHERE PatientID LIKE @Patient_ID" +
                    " AND UniversalTestID LIKE @Test_ID AND SpecimenID LIKE @Sample_Number AND PendingSending = 1;";


               using (DbCommand command = conn.CreateCommand())
               {
                    command.CommandText = selectPatientsQuery;
                    AddWithValue(command, "@Sample_Number", SampleNumber);
                    AddWithValue(command, "@Test_ID", testID);
                    DbDataReader patientReader = command.ExecuteReader();
                    if (!patientReader.HasRows)
                    {
                         return Results.Json(patientRequests);
                    }
                    else
                    {
                         while (patientReader.Read())
                         {
                              PatientRequest patient = new();
                              patient.Elements["Sequence#"] = $"{patientRequests.Count + 1}";
                              for (int i = 0; i < patientReader.FieldCount; i++)
                              {
                                   string fieldName = patientReader.GetName(i);
                                   switch (fieldName)
                                   {
                                        case "PatientID":
                                             patient.PatientID = patientReader.GetInt32(i);
                                             break;
                                        default:
                                             patient.Elements[fieldName] = $"{patientReader[fieldName]}";
                                             break;
                                   }
                              }
                              patientRequests.Add(patient);
                         }
                    }

                    patientReader.Close();
               }
               foreach (var patient in patientRequests)
               {
                    using (DbCommand orderCommand = conn.CreateCommand())
                    {
                         orderCommand.CommandText = selectOrdersQuery;
                         AddWithValue(orderCommand, "@Patient_ID", patient.PatientID);
                         AddWithValue(orderCommand, "@Sample_Number", SampleNumber);
                         AddWithValue(orderCommand, "@Test_ID", testID);
                         DbDataReader orderReader = orderCommand.ExecuteReader();
                         while (orderReader.Read())
                         {
                              OrderRequest order = new((PatientRequest)patient);
                              order.Elements["Sequence#"] = $"{patient.Orders.Count + 1}";
                              for (int i = 0; i < orderReader.FieldCount; i++)
                              {
                                   string fieldName = orderReader.GetName(i);
                                   switch (fieldName)
                                   {
                                        case "OrderID":
                                             order.OrderID = Convert.ToInt32(orderReader[fieldName]);
                                             break;
                                        case "PatientID":
                                        case "PendingSending":
                                             break;
                                        default:
                                             order.Elements[fieldName] = $"{orderReader[fieldName]}";
                                             break;
                                   }
                              }
                              patient.Orders.Add(order);
                         }
                         orderReader.Close();
                    }
                    foreach (var order in patient.Orders)
                    {
                         using DbCommand command = conn.CreateCommand();
                         command.CommandText = "UPDATE OrderRequest SET PendingSending = 0 WHERE OrderID = @RequestID";
                         AddWithValue(command, "@RequestID", order.OrderID);
                         command.ExecuteNonQuery();

                    }
               }

               return Results.Json(patientRequests);
          }

          public IResult AddResults(List<Patient> patientResults)
          {
               long pID;
               long oID;
               // This is where we have to connect to the database.
               const string NEW_PATIENT = "INSERT INTO PatientRecord (PracticePatientID, LabPatientID, PatientID3, PatientName, MMName, DOB, Sex, Race, Address, Reserved, TelNo," +
                              " AttendingPhysicianID, Special1, Special2, Height, Weight, Diagnosis, ActiveMeds, Diet, PF1, PF2, AdmDates, AdmStatus, Location, AltCodeNature, AltCode, Religion," +
                              " MaritalStatus, IsolationStatus, Language, HospService, HospInstitution, DosageCategory) VALUES (@PracticePatientID, @LabPatientID, @PatientID3, @PatientName," +
                              " @MMName, @DOB, @Sex, @Race, @Address, @Reserved, @TelNo, @AttendingPhysicianID, @Special1, @Special2, @Height, @Weight, @Diagnosis, @ActiveMeds, @Diet, @PF1, @PF2," +
                              " @AdmDates, @AdmStatus, @Location, @AltCodeNature, @AltCode, @Religion, @MaritalStatus, @IsolationStatus, @Language, @HospService, @HospInstitution, @DosageCategory);";
               const string NEW_ORDER = "INSERT INTO OrderRecord (PatientID, SpecimenID, InstrSpecID, UniversalTestID, Priority, OrderDate, CollectionDate, CollectionEndTime," +
                    " CollectionVolume, CollectorID, ActionCode, DangerCode, RelevantClinicInfo, SpecimenRecvd, SpecimenDescriptor, OrderingPhysician, PhysicianTelNo, UF1, UF2, LF1, LF2," +
                    " LastReported, BillRef, InstrSectionID, ReportType, Reserved, SpecCollectLocation, NosInfFlag, SpecService, SpecInstitution) VALUES (@Patient_ID, @SpecimenID," +
                    " @InstrSpecID, @UniversalTestID, @Priority, @OrderDate, @CollectionDate, @CollectionEndTime, @CollectionVolume, @CollectorID, @ActionCode, @DangerCode, @RelevantClinicInfo," +
                    " @SpecimenRecvd, @SpecimenDescriptor, @OrderingPhysician, @PhysicianTelNo, @UF1, @UF2, @LF1, @LF2, @LastReported, @BillRef, @InstrSectionID, @ReportType, @Reserved," +
                    " @SpecCollectLocation, @NosInfFlag, @SpecService, @SpecInstitution);";
               const string NEW_RESULT = "INSERT INTO ResultRecord (OrderID, UniversalTestID, ResultValue, Unit, RefRange, Abnormal, AbNature, ResStatus, NormsChanged, OperatorID, TestStart," +
                    " TestEnd, InstrumentID) VALUES (@Order_ID, @UniversalTestID, @ResultValue, @Unit, @RefRange, @Abnormal, @AbNature, @ResStatus, @NormsChanged, @OperatorID, @TestStart, @TestEnd, @InstrumentID);";
               foreach (var patient in patientResults)
               {
                    /* * Patient fields: * *
                     *  Note: Fields marked with "=/=" are listed in the Siemens interface specification as not officially supported.
                     *  These fields may or may not actually be functional (see the instrument's specification for details)
                     *  so initial development efforts will focus primarily on known supported fields.
                     * 0 Record Type
                     * 1 Sequence# Definition
                     * 2 Practice Assigned PatientID3
                     * 3 LabPatientID  =/=
                     * 4 PatientID3  =/=
                     * 5 PatientName
                     * 6 MMName  =/=
                     * 7 DOB *
                     * 8 Patient's Sex
                     * 9 Race-Ethnic Origin  =/=
                     *10 Patient's Address  =/=
                     *11 Reserved  =/=
                     *12 Patient's Phone#  =/=
                     *13 AttendingPhysicianID  =/=
                     *14 Special1  =/=
                     *15 Special2  =/=
                     *16 Height  =/=
                     *17 Weight  =/=
                     *18 Diagnosis  =/=
                     *19 ActiveMeds  =/=
                     *20 Diet  =/=
                     *21 PF1  =/=
                     *22 PF2  =/=
                     *23 AdmDates  =/= *
                     *24 AdmStatus  =/=
                     *25 Location  =/=
                     *26 AltCodeNature  =/=
                     *27 AltCode  =/=
                     *28 Religion  =/=
                     *29 MaritalStatus  =/=
                     *30 IsolationStatus  =/=
                     *31 Language  =/=
                     *32 HospService  =/=
                     *33 HospInstitution  =/=
                     *34 DosageCategory  =/=
                     */
                    // Insert a patient record to the PatientRecord table,
                    // grab the row ID, and use it to add order records to the OrderRecord table.
                    using DbConnection conn = new SqliteConnection(Database.GetConnectionString());
                    using (DbCommand command = conn.CreateCommand())
                    {
                         command.CommandText = NEW_PATIENT;
                         foreach (DictionaryEntry element in patient.Elements)
                         {
                              switch (element.Key)
                              {
                                   case "FrameNumber":
                                   case "Sequence#":
                                        break;
                                   default:
                                        AddWithValue(command, $"@{element.Key}", $"{element.Value}" == "" ? DBNull.Value : $"{element.Value}");
                                        break;
                              }
                         }
                         command.ExecuteNonQuery();
                    }
                    using (DbCommand command = conn.CreateCommand())
                    {
                         command.CommandText = LAST_INSERTED;
                         pID = (long)(command.ExecuteScalar() ?? 0);
                    }
                    foreach (Order order in patient.Orders)
                    {
                         /*
                         *  * Order Fields: * *
                         *  Note: Fields marked with "=/=" are listed in the Siemens interface specification as not officially supported.
                         *  These fields may or may not actually be functional.
                         *	0	Record Type (O)
                         *	1	Sequence#
                         *	2	SpecimenID
                         *	3	InstrSpecID
                         *	4	UniversalTestID
                         *	5	Priority
                         *	6	OrderDate*
                         *	7	CollectionDate*
                         *	8	CollectionEndTime*
                         *	9	CollectionVolume
                         *	10	CollectorID
                         *	11	ActionCode
                         *	12	DangerCode
                         *	13	RelevantClinicInfo
                         *	14	SpecimenRecvd*
                         *	15	SpecimenDescriptor,Specimen Type,Specimen Source
                         *	16	OrderingPhysician
                         *	17	PhysicianTelNo
                         *	18	UF1
                         *	19	UF2
                         *	20	LF1
                         *	21	LF2
                         *	22	LastReported*
                         *	23	BillRef
                         *	24	InstrSectionID
                         *	25	ReportType
                         *	26	Reserved
                         *	27	SpecCollectLocation
                         *	28	NosInfFlag
                         *	29	SpecService
                         *	30	SpecInstitution
                          */
                         using (DbCommand command = conn.CreateCommand())
                         {
                              command.CommandText = NEW_ORDER;
                              AddWithValue(command, "@Patient_ID", pID);
                              IDictionaryEnumerator enumerator = order.Elements.GetEnumerator();
                              while (enumerator.MoveNext())
                              {
                                   switch (enumerator.Key)
                                   {
                                        case "FrameNumber":
                                        case "Sequence#":
                                             break;
                                        default:
                                             AddWithValue(command, $"@{enumerator.Key}", $"{enumerator.Value}" == "" ? DBNull.Value : $"{enumerator.Value}");
                                             break;
                                   }
                              }
                              command.ExecuteNonQuery();
                         }
                         using (DbCommand command = conn.CreateCommand())
                         {
                              command.CommandText = LAST_INSERTED;
                              oID = (long)(command.ExecuteScalar() ?? 0);
                         }
                         // Use the row ID from each of those order records to add
                         // result records to the IMM_Results table for each Patient.Order.Result in the message.
                         foreach (var result in order.Results)
                         {
                              /* * Result Fields: * *
                              *  Note: Fields marked with "=/=" are listed in the Siemens interface specification as not officially supported.
                              *  These fields may or may not actually be functional.
                              *	0	Record Type (R)
                              *	1	Sequence#
                              *	2	UniversalTestID
                              *	3	Result
                              *	4	Unit
                              *	5	ReferenceRanges
                              *	6	Abnormal
                              *	7	AbNature
                              *	8	ResStatus
                              *	9	NormsChanged
                              *	10	OperatorID
                              *	11	Date\Time Test Started
                              *	12	Date\Time Test Completed
                              *	13	InstrumentID
                               */
                              using (DbCommand command = conn.CreateCommand())
                              {
                                   command.CommandText = NEW_RESULT;
                                   AddWithValue(command, "@Order_ID", oID);
                                   IDictionaryEnumerator enumerator = result.Elements.GetEnumerator();
                                   while (enumerator.MoveNext())
                                   {
                                        switch (enumerator.Key)
                                        {
                                             case "InstrumentID":
                                                  char[] trimmings = { '\x03', '\x0D' };
                                                  AddWithValue(command, $"@{enumerator.Key}", $"{enumerator.Value}" == "" ? DBNull.Value : $"{enumerator.Value}".Trim(trimmings));
                                                  break;
                                             case "FrameNumber":
                                             case "Sequence#":
                                                  break;
                                             default:
                                                  AddWithValue(command, $"@{enumerator.Key}", $"{enumerator.Value}" == "" ? DBNull.Value : $"{enumerator.Value}");
                                                  break;
                                        }
                                   }
                                   command.ExecuteNonQuery();
                              }
                         }
                    }
               }
               return Results.Accepted();
          }

          public IResult SetPending(List<PatientRequest> patientRequests)
          {
               using (DbConnection conn = new SqliteConnection(Database.GetConnectionString()))
               {
                    foreach (PatientRequest patientItem in patientRequests)
                    {
                         foreach (OrderRequest orderItem in patientItem.Orders)
                         {
                              using (DbCommand command = conn.CreateCommand())
                              {
                                   command.CommandText = "UPDATE OrderRequest SET PendingSending = 1 WHERE OrderID = @OrderID";
                                   DbParameter parameter = command.CreateParameter();
                                   parameter.ParameterName = "@OrderID";
                                   parameter.Value = orderItem.OrderID;
                                   command.Parameters.Add(parameter);
                                   command.ExecuteNonQuery();
                              }
                         }
                    }
               }
               return Results.Ok();
          }

          private static void AddWithValue(DbCommand command, string parameterName, object? value)
          {
               DbParameter parameter = command.CreateParameter();
               parameter.ParameterName = parameterName;
               parameter.Value = value;
               command.Parameters.Add(parameter);
          }

     }
}