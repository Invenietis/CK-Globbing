using Cake.Common.IO;
using Cake.Core;

namespace CodeCake
{
    public partial class Build : CodeCakeHost
    {
        public Build()
        {

            StandardGlobalInfo globalInfo = CreateStandardGlobalInfo()
                                                .AddDotnet()
                                                .SetCIBuildTag();

            Task( "Check-Repository" )
                .Does( () =>
                {
                    globalInfo.TerminateIfShouldStop();
                } );

            Task( "Clean" )
                .IsDependentOn( "Check-Repository" )
                .Does( () =>
                {
                    globalInfo.GetDotnetSolution().Clean();
                    Cake.CleanDirectories( globalInfo.ReleasesFolder.ToString() );
                } );

            Task( "Build" )
                .IsDependentOn( "Clean" )
                .Does( () =>
                {
                    globalInfo.GetDotnetSolution().Build();
                } );

            Task( "Unit-Testing" )
                .IsDependentOn( "Build" )
                .WithCriteria( () => Cake.InteractiveMode() == InteractiveMode.NoInteraction
                                     || Cake.ReadInteractiveOption( "RunUnitTests", "Run Unit Tests?", 'Y', 'N' ) == 'Y' )
                .Does( () =>
                {
                    globalInfo.GetDotnetSolution().Test();
                } );

            Task( "Create-NuGet-Packages" )
                .IsDependentOn( "Unit-Testing" )
                .Does( () =>
                {
                    globalInfo.GetDotnetSolution().Pack();
                } );

            Task( "Push-NuGet-Packages" )
                .IsDependentOn( "Create-NuGet-Packages" )
                .WithCriteria( () => globalInfo.IsValid )
                .Does( async () =>
                {
                    await globalInfo.PushArtifactsAsync();
                } );

            // The Default task for this script can be set here.
            Task( "Default" )
                .IsDependentOn( "Push-NuGet-Packages" );
        }
    }
}
