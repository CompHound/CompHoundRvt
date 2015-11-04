using RestSharp;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Web;
using System.Collections.Generic;
using System.Collections;
using System.Windows.Forms;
using MultiFileUploader.Models;
using System.Diagnostics;

// Requires references to the following assemblies:
// - System.Web
// - System.Windows.Forms
// - Newstonsoft.Json (NuGet package)
// - RestSharp (NuGet package)

namespace MultiFileUploader
{
  public class Util
  {
    string baseUrl = "";
    RestClient m_client;

    public static AccessToken token;
    public static DateTime issueDateTime;

    // Refresh token if it is about to expire.

    public static int ABOUT_EXPIRED_SECONDS = 10;

    TextBox globalUploadingFileTxtBox = null;

    public Util( string baseUrl )
    {
      this.baseUrl = baseUrl;
      m_client = new RestClient( baseUrl );
    }

    public AccessToken GetAccessToken( string clientId, string clientSecret )
    {
      //no token or token is going to be expired 
      // (less than ABOUT_EXPIRED_SECONDS)

      if( token == null
        || ( DateTime.Now - issueDateTime ).TotalSeconds
          > ( token.expires_in - ABOUT_EXPIRED_SECONDS ) )
      {
        RestRequest req = new RestRequest();
        req.Resource = "authentication/v1/authenticate";
        req.Method = Method.POST;
        req.AddHeader( "Content-Type", "application/x-www-form-urlencoded" );
        req.AddParameter( "client_id", clientId );
        req.AddParameter( "client_secret", clientSecret );
        req.AddParameter( "grant_type", "client_credentials" );
        //avoid CORS issue, do not use this if you just need to
        //get the access token from same domain
        req.AddHeader( "Access-Control-Allow-Origin", "*" );

        IRestResponse<AccessToken> resp = m_client.Execute<AccessToken>( req );
        //logger.Debug(resp.Content);

        if( resp.StatusCode == System.Net.HttpStatusCode.OK )
        {
          AccessToken ar = resp.Data;
          if( ar != null )
          {
            token = ar;
            //update the token issue time
            issueDateTime = DateTime.Now;
          }
        }
        else
        {
          //logger.Fatal("Authentication failed! clientId:" + clientId);
        }
      }
      else
      {
        ;//Do nothing, use the saved access token in static var 
      }
      return token;
    }

    public bool IsBucketExist( string defaultBucketKey, string accessToken )
    {
      RestRequest req = new RestRequest();
      req.Resource = "oss/v1/buckets" + "/" + defaultBucketKey + "/details";
      req.Method = Method.GET;
      req.AddParameter( "Authorization", "Bearer " + accessToken, ParameterType.HttpHeader );
      req.AddParameter( "Content-Type", "application/json", ParameterType.HttpHeader );

      IRestResponse<BucketDetails> resp = m_client
          .Execute<BucketDetails>( req );

      //logger.Debug(resp.Content);
      return resp.StatusCode == System.Net.HttpStatusCode.OK;
    }

    public BucketDetails GetBucketDetails( string defaultBucketKey, string accessToken )
    {
      RestRequest req = new RestRequest();
      req.Resource = "oss/v1/buckets" + "/" + defaultBucketKey + "/details";
      req.Method = Method.GET;
      req.AddParameter( "Authorization", "Bearer " + accessToken, ParameterType.HttpHeader );
      req.AddParameter( "Content-Type", "application/json", ParameterType.HttpHeader );

      IRestResponse<BucketDetails> resp
        = m_client.Execute<BucketDetails>( req );

      if( resp.StatusCode == System.Net.HttpStatusCode.OK )
      {
        return resp.Data;
      }
      else
      {
        //logger.Error("GetBucketDetails error. http code:" + resp.StatusCode);
        //logger.Debug(resp.Content);
        return null;
      }
    }

    public bool CreateBucket( string defaultBucketKey, string accessToken )
    {
      RestRequest req = new RestRequest();
      req.Resource = "oss/v1/buckets";
      req.Method = Method.POST;
      req.AddParameter( "Authorization", "Bearer " + accessToken, ParameterType.HttpHeader );
      req.AddParameter( "Content-Type", "application/json", ParameterType.HttpHeader );

      string body = "{\"bucketKey\":\"" + defaultBucketKey + "\",\"policy\":\"persistent\"}";

      req.AddParameter( "application/json", body, ParameterType.RequestBody );

      IRestResponse respBC = m_client.Execute( req );

      return ( respBC.StatusCode == System.Net.HttpStatusCode.OK );
    }

    public bool UploadFile( string bucketKey, string accessToken, string file, out string base64URN )
    {
      //Do not use HttpUtility.UrlEncode, it does not encode charactor '+' 
      //string objectKey = HttpUtility.UrlEncode(file.FileName);

      base64URN = String.Empty;

      string objectKey = Path.GetFileName( file );
      FileStream filestream;
      try
      {
        filestream = File.Open( file, FileMode.Open, FileAccess.Read );
      }
      catch( Exception ex )
      {
        Debug.Print( ex.Message );
        return false;
      }
      byte[] fileData = null;
      int nlength = (int) filestream.Length;
      using( BinaryReader reader = new BinaryReader( filestream ) )
      {
        fileData = reader.ReadBytes( nlength );
      }

      string contentType = MimeMapping.GetMimeMapping( file );

      RestRequest req = new RestRequest();
      ///oss/{api version}/buckets/{bucket key}/objects/{object key}
      req.Resource = "oss/v1/buckets/" + bucketKey + "/objects/" + objectKey;
      req.Method = Method.PUT;
      req.AddParameter( "Authorization", "Bearer " + accessToken, ParameterType.HttpHeader );
      req.AddParameter( "Content-Type", contentType );
      //req.AddParameter( "Content-Length", nlength ); // Cyrille says to never use this
      req.AddParameter( "requestBody", fileData, ParameterType.RequestBody );

      IRestResponse resp = m_client.Execute( req );
      if( resp.StatusCode == System.Net.HttpStatusCode.OK )
      {
        string content = resp.Content;

        //logger.Debug(content);
        //TODO: better way to get ID value from response with Object-Origented way
        var id = GetIdValueInJson( content );
        //ret_urn = id;
        base64URN = EncodeBase64( id );
      }
      else
      {
        MessageBox.Show( "Upload to bucket '" 
          + bucketKey + "' failed for file '" 
          + file + "'.", "CompHound", 
          MessageBoxButtons.OK, MessageBoxIcon.Stop );

        return false;
      }
      return true;
    }

    /// <summary>
    /// Upload files and set references
    /// </summary>
    public string UploadFilesAndSetReference(
      string bucketName,
      string accessToken,
      ArrayList files,
      string rootFile,
      bool showInBrowser )
    {
      // upload all files and record: root file name,root urn, refed file names, refed urns
      string root_urn = "";
      string real_root_file = "";

      Dictionary<string, string> map_filename_urn = new Dictionary<string, string>();
      if( files.Count > 0 )
      {
        int success_counter = 0;
        foreach( string file in files )
        {
          // Put the name of the file being uploaded 
          // in the text box on the main form
          if( globalUploadingFileTxtBox != null )
          {
            globalUploadingFileTxtBox.Text = file;
            globalUploadingFileTxtBox.Update();
          }
          else
          {
            System.Diagnostics.Debug.Print( file + "\n" );
          }

          // upload the file, 
          string file_urn;

          // upload success
          if( UploadFile( bucketName, accessToken, file, out file_urn ) )
          {
            // init root_urn with the first file as default
            if( success_counter == 0 )
            {
              ++success_counter;
              root_urn = file_urn;
              real_root_file = file;
            }
            // record the urn of the root file
            if( rootFile == file )
            {
              root_urn = file_urn;
              real_root_file = rootFile;
            }
            else
            {
              string xxfile = Path.GetFileName( file );
              //wb added try
              try
              {
                map_filename_urn.Add( xxfile, file_urn );
              }
              catch
              {
                continue;
              }
            }
          }
        }

        if( 0 == success_counter )
        {
          return null;
        }

        // set up the reference
        if( ( files.Count > 1 ) && ( map_filename_urn.Count > 0 ) )
        {
          RestRequest request = new RestRequest( "/references/v1/setreference", Method.POST );
          request.AddHeader( "Authorization", "Bearer " + accessToken );
          request.AddHeader( "Content-Type", "application/json" );

          StringWriter sw = new StringWriter();
          JsonTextWriter jsw = new JsonTextWriter( sw );
          jsw.WriteStartObject();
          jsw.WritePropertyName( "master" );
          jsw.WriteValue( Util.DecodeBase64( root_urn ) );
          jsw.WritePropertyName( "dependencies" );
          jsw.WriteStartArray();

          // int i = 0;
          foreach( string key in map_filename_urn.Keys )
          {
            jsw.WriteStartObject();
            jsw.WritePropertyName( "file" );
            jsw.WriteValue( Util.DecodeBase64( map_filename_urn[key] ) );
            jsw.WritePropertyName( "metadata" );
            jsw.WriteStartObject();
            jsw.WritePropertyName( "childPath" );
            jsw.WriteValue( key );
            jsw.WritePropertyName( "childName" );
            jsw.WriteValue( key );
            jsw.WritePropertyName( "parentPath" );
            string xx = Path.GetFileName( real_root_file );
            jsw.WriteValue( xx );
            jsw.WriteEndObject();
            jsw.WriteEndObject();
          }
          jsw.WriteEndArray();
          jsw.WriteEndObject();
          string body = sw.ToString();

          System.Diagnostics.Debug.Print( body + "\n" );

          request.AddParameter( "application/json", body, ParameterType.RequestBody );

          IRestResponse resp = m_client.Execute( request );
          if( resp.StatusCode == System.Net.HttpStatusCode.OK )
          {
            string content = resp.Content;
          }
        }
      }

      StartTranslation( root_urn, accessToken );

      // not using this during testing
      //GetTranslationProgress(_Form1.URN_TextBox.Text, _Form1.AuthCodeTextBox.Text);

      //Clipboard.SetDataObject("http://viewer.autodesk.io/node/view-helper/?" + "urn: " + root_urn + " token: " + accessToken);
      // MessageBox.Show("complete!");
      if( showInBrowser )
        LaunchBrowser( root_urn, accessToken );

      if( globalUploadingFileTxtBox != null )
      {
        globalUploadingFileTxtBox.Text = "Completed";
        globalUploadingFileTxtBox.Update();
      }
      else
      {
        System.Diagnostics.Debug.Print( "Completed\n" );
      }
      return root_urn;
    }

    public bool StartTranslation( string base64URN, string accessToken )
    {
      RestRequest req = new RestRequest();
      //Start translation,
      //viewingservice/v1/register
      req.Resource = "viewingservice/v1/register";
      req.Method = Method.POST;
      req.AddParameter( "Authorization", "Bearer " + accessToken, ParameterType.HttpHeader );
      req.AddParameter( "Content-Type", "application/json;charset=utf-8", ParameterType.HttpHeader );
      req.AddParameter( "x-ads-force", "true", ParameterType.HttpHeader );

      ////force regeneration
      //req.AddParameter("x-ads-force", "true",ParameterType.HttpHeader);
      //// will not trigger a real translation, just respond all parameters for translation.
      //req.AddParameter("x-ads-test", "true", ParameterType.HttpHeader);

      string body = "{\"urn\":\"" + base64URN + "\"}";
      req.AddParameter( "application/json", body, ParameterType.RequestBody );

      IRestResponse resp = m_client.Execute( req );
      string content = "";
      if( resp.StatusCode == System.Net.HttpStatusCode.OK )
      {
        content = resp.Content;
        //logger.Info(" Translation starting...");

        return true;
      }
      else if( resp.StatusCode == System.Net.HttpStatusCode.Created )
      {
        content = resp.Content;

        //logger.Info("Translation has been posted before, it is ready for viewing");
        return true;
      }
      else
      {

        //logger.Error("error when trying to tranlate. http code:" + resp.StatusCode);
        //logger.Debug(resp.Content);
        return false;
      }
    }

    public string GetTranslationProgress( string base64URN, string accessToken )
    {
      string percentage = "0%";
      string status = "";
      RestRequest req = new RestRequest();

      string resource = string.Format( "viewingservice/v1/{0}", base64URN );
      req.Resource = resource;
      req.Method = Method.GET;
      req.AddParameter( "Authorization", "Bearer " + accessToken, ParameterType.HttpHeader );
      req.AddParameter( "Content-Type", "application/json;charset=utf-8", ParameterType.HttpHeader );
      ////force regeneration
      //req.AddParameter("x-ads-force", "true",ParameterType.HttpHeader);
      //// will not trigger a real translation, just respond all parameters for translation.
      //req.AddParameter("x-ads-test", "true", ParameterType.HttpHeader);

      IRestResponse<BubbleStatus> resp = m_client.Execute<BubbleStatus>( req );
      if( resp.StatusCode == System.Net.HttpStatusCode.OK
          && resp.Data != null )
      {
        BubbleStatus bt = resp.Data;

        percentage = bt.progress;

        status = bt.status;
      }
      else
      {
        //logger.Error("error when getting progress. http code:" + resp.StatusCode);
        //logger.Debug(resp.Content);
      }
      // _Form1.Status_richTextBox1.Text = status; // percentage;
      return percentage;
    }

    public string UploadFilesWithReferences(
      string clientID, string clientSecret, string bucketName,
      ArrayList ar, TextBox uploadingFileTxtBox, bool showInBrowser )
    {
      globalUploadingFileTxtBox = uploadingFileTxtBox;

      // top level file
      string topFilePath = ar[0].ToString();
      string topFileName = System.IO.Path.GetFileName( topFilePath );
      if( topFileName.IndexOf( ' ' ) >= 0 )
      {
        MessageBox.Show( "File name should not cotain a space as the translation process does not seem to like that!" );
        return "";
      }

      // This will get the access token. (It will set the AccessToken member variable token to a usuable value)        
      GetAccessToken( clientID, clientSecret );

      // Make sure the bucket exists - the access token is needed for the call to OSS
      CreateBucket( bucketName, token.access_token );

      // This will be the URN of the assembly will use it to view the assembly in a browser
      string topLevelFileURN = "";

      // passing in the bucket name, the access token and the array of file paths, and the top level assembly name 
      topLevelFileURN = UploadFilesAndSetReference(
        bucketName, token.access_token, ar, ar[0].ToString(), showInBrowser );

      return topLevelFileURN;
    }

    private static string GetIdValueInJson( string content )
    {
      string idSrcFlag = "\"id\" : \"";
      int index = content.IndexOf( idSrcFlag ) + idSrcFlag.Length;
      int idLen = content.IndexOf( "\"", index + 1 ) - index;
      var urn = content.Substring( index, idLen );
      return urn;
    }

    public static string DecodeBase64( string base64EncodedData )
    {
      byte[] bytes = Convert.FromBase64String( base64EncodedData );
      return Encoding.UTF8.GetString( bytes );
    }

    public static string EncodeBase64( string plainText )
    {
      byte[] bytes = Encoding.UTF8.GetBytes( plainText );
      return Convert.ToBase64String( bytes );
    }

    public void LaunchBrowser( string URN, string AuthCode )
    {
      string url = string.Format(
        "http://viewer.autodesk.io/node/view-helper?urn={0}&token={1}",
        HttpUtility.UrlEncode( URN ),
        AuthCode );

      System.Diagnostics.Process.Start( url );
    }
  }
}
