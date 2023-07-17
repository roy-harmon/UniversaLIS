using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using UniversaLIS;
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
               builder.Services.AddHostedService<UniversaLIService>();
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
                    using (PatientDB dB = new PatientDB())
                    {
                         return dB.GetPatients(pPID, ppN, pDOB, puTID);
                    }
               }).WithName("GetPatients").Produces<List<Patient>>(StatusCodes.Status200OK);

               // Get a specific patient record by ID.
               app.MapGet("/reports/patients/{id}", (int id) =>
               {
                    using (PatientDB dB = new PatientDB())
                    {
                         return dB.GetPatient(id);
                    }
               }).WithName("GetPatientByID").Produces<Patient>(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound);

               // Endpoints for requests sent to the LIS for distribution to testing instrumentation. 

               // Top level is patient requests.
               // POST new requests here to submit them for testing. Include orders.
               app.MapPost("/requests/patients", (PatientRequest patientRequest) =>
               {
                    using (PatientDB dB = new PatientDB())
                    {
                         return dB.PostPatientRequest(patientRequest);
                    }
               }).WithName("PostPatientOrders").Produces(StatusCodes.Status201Created).Produces(StatusCodes.Status400BadRequest);

               // GET a list of all patient requests. 
               // Supports querying by PracticePatientID, PatientName, DOB, and UniversalTestID fields.
               app.MapGet("/requests/patients", ([FromQuery] string? practicePatientID, [FromQuery] string? patientName,
               [FromQuery] string? DOB, [FromQuery] string? universalTestID) =>
               {
                    string pPID = practicePatientID == null ? "%" : practicePatientID;
                    string ppN = patientName == null ? "%" : patientName;
                    string pDOB = DOB == null ? "%" : DOB;
                    string puTID = universalTestID == null ? "^" : universalTestID;
                    using (PatientDB dB = new PatientDB())
                    {
                         return dB.GetPatientRequests(pPID, ppN, pDOB, puTID);
                    }
               }).WithName("GetPatientRequests").Produces<List<PatientRequest>>(StatusCodes.Status200OK);

               // Get a specific patient request by ID.
               app.MapGet("/requests/patients/{id}", (int id) =>
               {
                    using (PatientDB dB = new PatientDB())
                    {
                         return dB.GetPatientRequestById(id);
                    }
               }).WithName("GetPatientOrdersByID").Produces<PatientRequest>(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound);

               // Update a specific patient request by ID.
               app.MapPut("/requests/patients/{id}", (int id,
                    [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Disallow)] PatientRequest patient) =>
               {
                    using (PatientDB dB = new PatientDB())
                    {
                         return dB.PutPatientRequestById(id, patient);
                    }
               }).WithName("UpdatePatientOrderByID").Produces<PatientRequest>(StatusCodes.Status200OK).Produces(StatusCodes.Status400BadRequest).Produces(StatusCodes.Status404NotFound);

               // Delete a specific patient request by ID.
               app.MapDelete("/requests/patients/{id}", (int id) =>
               {
                    using (PatientDB dB = new PatientDB())
                    {
                         return dB.DeletePatientRequestById(id);
                    }
               }).WithName("DeletePatientRequestByID").Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound);
               app.Run();
          }
     }

     class PatientDB : DbContext
     {
          public PatientDB() { }

          readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions()
          {
               DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
               ReferenceHandler = ReferenceHandler.IgnoreCycles
          };

          protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
          {
               optionsBuilder.UseSqlite("DataSource=../UniversaLIS/internal.db");
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

     }
}