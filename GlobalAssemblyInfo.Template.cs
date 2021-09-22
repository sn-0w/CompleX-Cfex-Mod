using System.Reflection;

[assembly: AssemblyProduct("CompleX")]
[assembly: AssemblyCompany("Krawk")]
[assembly: AssemblyCopyright("Copyright (C) Krawk 2019")]

#if DEBUG

[assembly: AssemblyConfiguration("Debug")]
#else

[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyVersion("1.0.0")]
[assembly: AssemblyFileVersion("1.0.0")]
[assembly: AssemblyInformationalVersion("v1.0.0")]