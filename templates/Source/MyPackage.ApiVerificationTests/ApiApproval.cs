using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Pathy;
using PublicApiGenerator;
using VerifyTests;
using VerifyTests.DiffPlex;
using VerifyXunit;
using Xunit;

namespace MyPackage.ApiVerificationTests;

public class ApiApproval
{
    private static readonly ChainablePath SourcePath = ChainablePath.Current / ".." / ".." / ".." / "..";

    static ApiApproval()
    {
        VerifyDiffPlex.Initialize(OutputType.Minimal);
    }

    [Theory]
    [ClassData(typeof(TargetFrameworksTheoryData))]
    public Task ApproveApi(string framework)
    {
        var configuration = typeof(ApiApproval).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
        var assemblyFile = SourcePath / "MyPackage" / "bin" / configuration / framework / "MyPackage.dll";
        var assembly = Assembly.LoadFile(assemblyFile);
        var publicApi = assembly.GeneratePublicApi(options: null);

        return Verifier
            // The trailing newline keeps this in sync with the *.verified.txt files, which always end
            // with a newline because the build's template rendering step normalizes text files that way.
            .Verify(publicApi + "\n")
            .ScrubLinesContaining("FrameworkDisplayName")
            .UseDirectory("ApprovedApi")
            .UseFileName(framework)
            .DisableDiff();
    }

    private class TargetFrameworksTheoryData : TheoryData<string>
    {
        public TargetFrameworksTheoryData()
        {
            var csproj = SourcePath / "MyPackage" / "MyPackage.csproj";
            var project = XDocument.Load(csproj);
            var targetFrameworks = project.XPathSelectElement("/Project/PropertyGroup/TargetFrameworks");
            AddRange(targetFrameworks!.Value.Split(';'));
        }
    }
}
