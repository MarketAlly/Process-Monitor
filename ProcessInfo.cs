using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace marketally.processmonitor
{
	class ProcessInfo
	{
		public string Name { get; set; }
		public string Path { get; set; }
		public Int32 Count { get; set; }
		public string Time { get; set; }
		public int? Interval { get; set; }
		public bool Enable { get; set; }
		public DateTime? LastRun { get; set; }
	}
}
