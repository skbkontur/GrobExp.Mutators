#tool "nuget:?package=NUnit.Runners&version=2.6.4"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");
var slnPath = "./GrobExp.Mutators.sln";

Task("Default")
    .Does(() =>
{
    Information(@"Supported cake targets:
        Restore-NuGet-Packages : Runs NuGet to restore all required packages.
        Rebuild : Rebuild grobuf to ./Assemblies/
        Run-Unit-Tests : Rebuild and run unit tests excluding 'LongRunning' category.
        Run-All-Tests : Rebuild and run all unit tests.
    ");
});

Task("Restore-NuGet-Packages")
    .Does(() =>
{
    var solutions = GetFiles("./*.sln");
    foreach(var solution in solutions)
    {
        Information("Restoring {0}", solution);
        NuGetRestore(solution);
    }
});

Task("Rebuild")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() => 
{
    Information("Cleaning output directory.");
    CleanDirectory("./Output/");
    
    Information(@"Rebuilding GrobExp solution. Configuration : {0}", configuration);
    MSBuild(slnPath, settings => settings.WithTarget("Rebuild")
                                         .SetVerbosity(Verbosity.Minimal)
                                         .SetConfiguration(configuration)
                                         .UseToolVersion(MSBuildToolVersion.VS2017));

    Information("Copying build results to output directory");
    EnsureDirectoryExists("./Output/bin/");
    CopyDirectory($"./Mutators/bin/{configuration}/", "./Output/bin/");
});

Task("Run-Tests")
    .IsDependentOn("Rebuild")
    .Does(() =>
{
    Information("Running unit tests.");
    RunTests(false);
});              

Task("Run-Tests-Except-Known-Failing")
    .IsDependentOn("Rebuild")
    .Does(() =>
{
    Information("Running unit tests.");
    RunTests(true);
});

RunTarget(target);

private void RunTests(bool excludeKnownFailing){
    EnsureDirectoryExists("./Output/test/");
    var settings = new NUnitSettings
    {
        NoResults = true,
        OutputFile = "./Output/test/tests-output.txt"
    };
    if(excludeKnownFailing)
        settings.Exclude = "Failing";
    NUnit($"./Mutators.Tests/bin/{configuration}/*.Tests.dll", settings);
}