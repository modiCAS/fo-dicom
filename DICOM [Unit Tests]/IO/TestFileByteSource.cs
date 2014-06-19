using System.Diagnostics.CodeAnalysis;
using System.IO;
using Dicom.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DICOM__Unit_Tests_.IO {
	/// <summary>
	/// Unit tests for the file byte source.
	/// </summary>
	[TestClass, ExcludeFromCodeCoverage]
	public class TestFileByteSource : TestIByteSource<FileByteSource> {
		/// <summary>
		/// Test what happens if someone tries to dispose the source twice.
		/// </summary>
		[TestMethod]
		public void TestDoubleDispose() {
			var source = CreateByteSource(256);
			source.Dispose();
			source.Dispose();
		}

		protected override FileByteSource CreateByteSource(int length) {
			if (!TestData.IsAvailable(length))
				Assert.Inconclusive("No test data file {0} available.", TestData.GetPath(length));
			return new FileByteSource(new FileReference(TestData.GetPath(length)));
		}

		protected override string GetSourceName() {
			return "file";
		}

		protected override void ReleaseByteSource(FileByteSource source) {
			source.Dispose();
		}
	}
}