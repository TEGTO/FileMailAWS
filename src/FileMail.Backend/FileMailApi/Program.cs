using Amazon.S3;
using FileMailApi;
using FileMailApi.Services;
using FluentValidation;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining(typeof(Program));
ValidatorOptions.Global.LanguageManager.Enabled = false;

var s3Client = new AmazonS3Client();
builder.Services.AddSingleton<IAmazonS3>(s3Client);

builder.Services.AddSingleton<IUploadFileService, UploadFileService>();

#region Cors

bool.TryParse(builder.Configuration[ConfigurationVariableKeys.UseCORS], out bool useCORS);
var myAllowSpecificOrigins = "_myAllowSpecificOrigins";

if (useCORS)
{
    builder.Services.AddApplicationCORS(builder.Configuration, myAllowSpecificOrigins, builder.Environment.IsDevelopment());
}

#endregion

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");

app.UseCors(myAllowSpecificOrigins);
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();