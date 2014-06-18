using System.Diagnostics.CodeAnalysis;
using Dicom.IO;
using Dicom.IO.Buffer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DICOM__Unit_Tests_.IO.Buffer {
	[TestClass, ExcludeFromCodeCoverage]
	public class TestByteBufferByteSource : TestIByteSource<ByteBufferByteSource> {
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

		[TestMethod]
		public void TestVariableWithCallback() {
			var source = new ByteBufferByteSource();
			Assert.IsFalse(source.Require(4, TestCallback, null));
		}

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