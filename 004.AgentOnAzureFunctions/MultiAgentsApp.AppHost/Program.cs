var builder = DistributedApplication.CreateBuilder(args);

var multiAgentsApp = builder.AddAzureFunctionsProject<Projects.MultiAgentsApp>("multiagentsapp")
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.MultiAgentsApp_Client>("multiagentsapp-client")
    .WithReference(multiAgentsApp)
    .WaitFor(multiAgentsApp);

builder.Build().Run();
