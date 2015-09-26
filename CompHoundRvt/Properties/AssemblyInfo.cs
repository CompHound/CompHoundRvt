using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle( "CompHoundRvt" )]
[assembly: AssemblyDescription( "Revit Add-In Description for CompHoundRvt" )]
[assembly: AssemblyConfiguration( "" )]
[assembly: AssemblyCompany( "Autodesk Inc." )]
[assembly: AssemblyProduct( "CompHoundRvt Revit Add-In" )]
[assembly: AssemblyCopyright( "Copyright 2015 © Jeremy Tammik Autodesk Inc." )]
[assembly: AssemblyTrademark( "" )]
[assembly: AssemblyCulture( "" )]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible( false )]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid( "321044f7-b0b2-4b1c-af18-e71a19252be0" )]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
//
// 2015-09-10 2016.0.0.0 initial implementation based on pre-RestSharp FireRating sample
// 2015-09-16 2016.0.0.1 upgrading towards post-RestSharp FireRating sample, implemented InstanceData class
// 2015-09-16 2016.0.0.2 completed upgrade to post-RestSharp FireRating sample https://github.com/jeremytammik/FireRatingCloud/releases/tag/2016.0.0.13
// 2015-09-26 2016.0.0.3 added view and data api urn and error handling for connection failure
// 2015-09-26 2016.0.0.4 improved error handling and updated to new node server port
//
[assembly: AssemblyVersion( "2016.0.0.4" )]
[assembly: AssemblyFileVersion( "2016.0.0.4" )]
