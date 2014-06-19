using System.Diagnostics.CodeAnalysis;
using Dicom.IO;
using Dicom.IO.Buffer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DICOM__Unit_Tests_.IO.Buffer {
	/// <summary>
	/// Unit tests for the byte buffer byte source.
	/// </summary>
	[TestClass, ExcludeFromCodeCoverage]
	public class TestByteBufferByteSource : TestIByteSource<ByteBufferByteSource> {
		/// <summary>
		/// Test what happens if more bytes were requested from the source than are available
		/// and no callback was provided.
		/// </summary>
		[TestMethod]
		public void TestVariableWithoutCallback() {
			var source = new ByteBufferByteSource();
			const int requestBytes = 4;
			try {
				source.Require(requestBytes);
				Assert.Inconclusive("Require should have failed");
			} catch (DicomIoException e) {
				Assert.AreEqual(string.Format(
					"Requested {0} bytes past end of byte source without providing a callback.", requestBytes), e.Message);
			}
		}

		/// <summary>
		/// Test what happens if more bytes were requested from the source than are available
		/// but a callback was provided.
		/// </summary>
		[TestMethod]
		public void TestVariableWithCallback() {
			var source = new ByteBufferByteSource();
			Assert.IsFalse(source.Require(4, TestCallback, null));
		}

		/// <summary>
		/// Callback dummy supplied to the sources Require method.
		/// </summary>
		/// <param name="source">Reference to the byte source.</param>
		/// <param name="state">State object.</param>
		private void TestCallback(IByteSource source, object state) {
			throw new System.NotImplementedException();
		}

		protected override ByteBufferByteSource CreateByteSource(int length) {
			return new ByteBufferByteSource(new MemoryByteBuffer(new byte[length]));
		}

		protected override string GetSourceName() {
			return "byte source";
		}
	}
}