using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiFileUploader.Models
{
  public class AccessToken
  {
    public string token_type { get; set; }
    public int expires_in { get; set; }
    public string access_token { get; set; }
  }

  public class ServicesAllowed
  {
    public string serviceId { get; set; }
    public string access { get; set; }
  }

  public class BucketDetails
  {
    public string key { get; set; }
    public string owner { get; set; }
    public DateTime createdDate { get; set; }
    public List<ServicesAllowed> permissions { get; set; }
    public string policy { get; set; }
  }

  public class BubbleStatus
  {
    public string guid { get; set; }
    public string type { get; set; }
    public string hasThumbnail { get; set; }
    public string progress { get; set; }
    public string startedAt { get; set; }
    public string status { get; set; }
    public string success { get; set; }
    public string urn { get; set; }
  }
}
