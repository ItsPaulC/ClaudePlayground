var builder = DistributedApplication.CreateBuilder(args);

// Add MongoDB
var mongodb = builder.AddMongoDB("mongodb")
    .WithDataVolume()
    .AddDatabase("ClaudePlayground");

// Add the API project
builder.AddProject<Projects.ClaudePlayground_Api>("api")
    .WithReference(mongodb)
    .WaitFor(mongodb);

builder.Build().Run();
