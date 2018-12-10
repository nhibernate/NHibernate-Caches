using System.IO;
using System.Text;

namespace NHibernate.Caches.StackExchangeRedis
{
	internal partial class NHibernateTextWriter : TextWriter
	{
		private readonly INHibernateLogger _logger;

		public NHibernateTextWriter(INHibernateLogger logger)
		{
			_logger = logger;
		}

		public override Encoding Encoding => Encoding.UTF8;

		public override void Write(string value)
		{
			if (value == null)
			{
				return;
			}

			_logger.Debug(value);
		}

		public override void WriteLine(string value)
		{
			if (value == null)
			{
				return;
			}
			_logger.Debug(value);
		}

		public override void WriteLine()
		{
		}
	}
}
