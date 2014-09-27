using System.Runtime.Serialization;

namespace SilentUpdater
{
	[DataContract]
	internal class Manifest
	{
		[DataMember]
		public string LatestVersion { get; set; }
		[DataMember]
		public string DistUrl { get; set; }
	}
}