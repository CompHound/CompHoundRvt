#region Namespaces
using RestSharp;
#endregion // Namespaces

namespace CompHoundRvt
{
  class Util
  {
    #region String Formatting
    /// <summary>
    /// Return a string for a real number
    /// formatted to two decimal places.
    /// </summary>
    public static string RealString( double a )
    {
      return a.ToString( "0.##" );
    }
    #endregion // String Formatting

    #region HTTP Access
    /// <summary>
    /// Timeout for HTTP calls.
    /// </summary>
    public static int Timeout = 1000;

    /// <summary>
    /// HTTP access constant to toggle between local and global server.
    /// </summary>
    public static bool UseLocalServer = true;

    // HTTP access constants.

    const string _base_url_local = "http://127.0.0.1:3001";
    const string _base_url_global = "http://comphound.herokuapp.com";
    const string _api_version = "api/v1";

    /// <summary>
    /// Return REST API base URL.
    /// </summary>
    public static string RestApiBaseUrl
    {
      get
      {
        return UseLocalServer
          ? _base_url_local
          : _base_url_global;
      }
    }

    /// <summary>
    /// Return REST API URI.
    /// </summary>
    public static string RestApiUri
    {
      get
      {
        return RestApiBaseUrl + "/" + _api_version;
      }
    }

    /// <summary>
    /// PUT JSON document data into 
    /// the specified mongoDB collection.
    /// </summary>
    public static string Put(
      string collection_name_and_id,
      object data )
    {
      var client = new RestClient( RestApiBaseUrl );

      //client.Authenticator = new HttpBasicAuthenticator(username, password);

      //var request = new RestRequest( "resource/{id}", Method.POST );
      //request.AddParameter( "name", "value" ); // adds to POST or URL querystring based on Method
      //request.AddUrlSegment( "id", "123" ); // replaces matching token in request.Resource

      var request = new RestRequest( _api_version + "/"
        + collection_name_and_id, Method.PUT );

      request.RequestFormat = DataFormat.Json;

      //request.AddBody( new { A = "foo", B = "bar" } ); // uses JsonSerializer
      request.AddBody( data ); // uses JsonSerializer

      // If you just want POST params instead (which 
      // would still map to your model and is a lot 
      // more efficient since there's no serialization 
      // to JSON) do this:
      //request.AddParameter("A", "foo");
      //request.AddParameter("B", "bar");

      // easily add HTTP Headers
      //request.AddHeader( "header", "value" );

      // add files to upload (works with compatible verbs)
      //request.AddFile( path );

      // execute the request
      IRestResponse response = client.Execute( request );

      var content = response.Content; // raw content as string

      return content;
    }
    #endregion // HTTP Access
  }
}
