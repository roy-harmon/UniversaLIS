using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using UniversaLIS.Models;
using Microsoft.EntityFrameworkCore;
using UniversaLIS;
using System.Linq;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

internal class UniLisApi
{
     public void Start()
     {
          var builder = WebApplication.CreateBuilder();
          builder.Services.AddEndpointsApiExplorer();
          builder.Services.AddSwaggerGen();
          builder.Services.AddSingleton<BackgroundService, UniversaLIService>();
          builder.Services.AddDbContext<PatientDB>(options =>
          {
               options.UseSqlite(CommFacilitator.INTERNAL_CONNECTION_STRING);
          });
          var app = builder.Build();
          app.UseSwagger();
          app.UseSwaggerUI();
          app.UseHttpsRedirection();

          app.MapGet("/", (Func<string>)(() => "Hello World!"));
          app.MapGet("/patients", (async (context) =>
          {
               using (PatientDB dB = new PatientDB())
               {
                    await dB.GetAllPatients(context).ExecuteAsync(context);
               }
          })).WithName("GetPatients");
          app.MapGet("/patients/{id}", (async (context) =>
          {
               using (PatientDB dB = new PatientDB())
               {
                    await dB.GetPatient(context).ExecuteAsync(context);
               }
          })).WithName("GetPatientByID");

          app.Run();
     }
}

class PatientDB : DbContext
{
     public PatientDB() { }

     protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
     {
          optionsBuilder.UseSqlite(CommFacilitator.INTERNAL_CONNECTION_STRING);
          base.OnConfiguring(optionsBuilder);
     }

     protected override void OnModelCreating(ModelBuilder modelBuilder)
     {
          modelBuilder.Entity<Patient>(p =>
          p.ToTable("PatientRecord").HasKey(p => p.PatientID));
          modelBuilder.Entity<Order>(o =>
          o.ToTable("OrderRecord").HasKey(o => o.OrderID));
          modelBuilder.Entity<Result>(r =>
          r.ToTable("ResultRecord").HasKey(r => r.ResultID));
          modelBuilder.Entity<PatientRequest>(pr =>
          pr.ToTable("PatientRequest"));
          modelBuilder.Entity<OrderRequest>(or =>
          or.ToTable("OrderRequest"));
          base.OnModelCreating(modelBuilder);
     }

     public PatientDB(DbContextOptions<PatientDB> options) : base(options) { }
     public DbSet<Patient> Patients => Set<Patient>();
     public DbSet<PatientRequest> PatientRequests => Set<PatientRequest>();

     private IResult GetPatientById (int id)
     {
          return Results.Ok(this.Patients.Where(p => p.PatientID.Equals(id)).Single().GetJsonString());
     }

     public IResult GetPatient(int id)
     {
          if (id > 0)
          {
               return GetPatientById(id);
          }
          return Results.NoContent();
     }

     public IResult GetPatient (HttpContext context)
     {
          int id = int.Parse($"{context.Request.RouteValues["id"]}");
          return GetPatient(id);
     }

     public IResult GetAllPatients (HttpContext context)
     {
          return Results.Ok(this.Patients.ToList<Patient>());
     }
}