using ClaudePlayground.Api.Endpoints;
using ClaudePlayground.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Infrastructure (includes Application services and MongoDB)
builder.Services.AddInfrastructure(builder.Configuration);

WebApplication app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map endpoint groups
app.MapBusinessEndpoints();

app.Run();
