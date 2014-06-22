using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Dicom;
using Dicom.IO;
using Dicom.IO.Buffer;
using Dicom.IO.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DICOM__Unit_Tests_.IO.Reader {
	/// <summary>
	/// Unit tests for the DICOM file reader class.
	/// </summary>
	[TestClass, ExcludeFromCodeCoverage]
	public class TestDicomFileReader {
		/// <summary>
		/// Test the begin read method on an empty blocking source.
		/// </summary>
		[TestMethod]
		public void TestReadFromBlockingSource() {
			DicomFile df = new DicomFile();
			var source = new ByteBufferByteSource();
			var reader = new DicomFileReader();

			object state = new object();
			IAsyncResult result = reader.BeginRead(source,
				new DicomDatasetReaderObserver(df.FileMetaInfo),
				new DicomDatasetReaderObserver(df.Dataset),
				TestCallback, state);
			Assert.IsNotNull(result);
		}

		/// <summary>
		/// Test the begin read method on a blocking byte source providing incomplete metadata asynchronously.
		/// </summary>
		[TestMethod]
		public void TestReadFromBlockingSourceWithIncompleteMetadataOnly() {
			DicomFile df = new DicomFile();
			var source = new ByteBufferByteSource();
			var reader = new DicomFileReader();
			
			object state = new object();
			IAsyncResult result = reader.BeginRead(source,
				new DicomDatasetReaderObserver(df.FileMetaInfo),
				new DicomDatasetReaderObserver(df.Dataset),
				TestCallback, state);
			Assert.IsNotNull(result);

			source.Add(new FileByteSource(new FileReference("[Test Data]/metaonly.dcm")).GetBuffer(0x15A), true);
		}

		/// <summary>
		/// Test the begin read method on a blocking byte source providing complete metadata asynchronously.
		/// </summary>
		[TestMethod]
		public void TestReadFromBlockingSourceWithCompleteMetadataOnly() {
			DicomFile df = new DicomFile();
			var source = new ByteBufferByteSource();
			var reader = new DicomFileReader();

			object state = new object();
			IAsyncResult result = reader.BeginRead(source,
				new DicomDatasetReaderObserver(df.FileMetaInfo),
				new DicomDatasetReaderObserver(df.Dataset),
				TestCallback, state);
			Assert.IsNotNull(result);

			source.Add(new FileByteSource(new FileReference("[Test Data]/metaonly.dcm")).GetBuffer(0x15B), true);
		}

		private void TestCallback(IAsyncResult ar) {
			throw new NotImplementedException();
		}
	}
}
