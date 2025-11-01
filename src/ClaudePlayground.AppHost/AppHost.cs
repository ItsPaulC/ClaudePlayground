var builder = DistributedApplication.CreateBuilder(args);

// Add MongoDB
var mongodb = builder.AddMongoDB("mongodb")
    .WithDataVolume()
    .AddDatabase("ClaudePlayground");

// Add Redis for caching
var redis = builder.AddRedis("redis")
    .WithDataVolume();

// Add the API project
builder.AddProject<Projects.ClaudePlayground_Api>("api")
    .WithReference(mongodb)
    .WithReference(redis)
    .WaitFor(mongodb)
    .WaitFor(redis);

builder.Build().Run();
