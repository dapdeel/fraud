using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Api.Data;
using Api.Models;
using Api.Interfaces;
using Api.Services;
using Microsoft.OpenApi.Models;
using Api.Services.Interfaces;
using Api.Services.TransactionTracing;
using Microsoft.AspNetCore.Http.Features;
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.Dashboard;


var builder = WebApplication.CreateBuilder(args);


builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024; // 2 GB
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 2L * 1024 * 1024 * 1024; // 2 GB
});
builder.Services.AddAuthorization();
builder.Services.AddControllers();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<GraphConfig>(builder.Configuration.GetSection("Graph"));

builder.Services.AddCors(options =>
   {
       options.AddPolicy("AllowAllOrigins",
           builder =>
           {
               builder.AllowAnyOrigin() // Replace with your specific origin
                      .AllowAnyHeader()
                      .AllowAnyMethod();
           });
   });

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
});

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    });

AddServices(builder);

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientPermission", policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
        //  .AllowCredentials();
    });
});


var app = builder.Build();
app.UseHangfireDashboard();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API v1"));
}
else
{
    app.UseSwagger();

}

app.UseCors("ClientPermission");
app.UseHttpsRedirection();

var dashboardOptions =
            new DashboardOptions
            {
                IgnoreAntiforgeryToken = true
            };
app.UseHangfireDashboard("/jobs", new DashboardOptions
{
    Authorization = new[] { new UseHangfireDashboardFilter() } // Allow everyone to access
});
//app.MapHangfireDashboard();

app.UseAuthentication();
app.UseCors("AllowAllOrigins");
app.UseAuthorization();
app.MapControllers();

app.Run();

void AddServices(WebApplicationBuilder builder)
{
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IObservatoryService, ObservatoryService>();
    builder.Services.AddScoped<IBankService, BankService>();
    builder.Services.AddScoped<ITransferService, TransferService>();
    builder.Services.AddScoped<ITransactionSummaryService, TransactionSummaryService>();
    builder.Services.AddTransient<IGraphService, JanusService>();
    builder.Services.AddSingleton<IQueuePublisherService, RabbitMqQueueService>();
    builder.Services.AddHostedService<TransferIngestConsumerService>();
    builder.Services.AddHostedService<FileReaderConsumerService>();
    builder.Services.AddScoped<ITransactionTracingService, TransactionService>();
    builder.Services.AddTransient<ITransactionIngestGraphService, TransactionIngestGraphService>();
    builder.Services.AddScoped<ITransactionTracingGraphService, TransactionTracingGraphService>();
    builder.Services.AddScoped<IElasticSearchService, ElasticSearchService>();
    builder.Services.AddHangfire(config =>
            {
                config.UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection"));
            });
    builder.Services.AddHangfireServer();
    // RecurringJob.AddOrUpdate("recurring-job-id", () => , Cron.Daily);

}