using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Dicom.IO;

namespace Dicom.IO.Reader {
	public enum DicomReaderResult {
		Processing,
		Success,
		Error,
		Stopped,
		Suspended
	}

	public interface IDicomReader {
		bool IsExplicitVR {
			get;
			set;
		}

		ReaderResult Status {
			get;
		}

		ReaderResult Read(IByteSource source, IDicomReaderObserver observer, DicomTag stop=null);

		IAsyncResult BeginRead(IByteSource source, IDicomReaderObserver observer, DicomTag stop, AsyncCallback callback, object state);
		ReaderResult EndRead(IAsyncResult result);
	}
}
