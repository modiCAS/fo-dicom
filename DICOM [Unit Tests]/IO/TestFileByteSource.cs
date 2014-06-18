using System.Diagnostics.CodeAnalysis;
using System.IO;
using Dicom.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DICOM__Unit_Tests_.IO {
	[TestClass, ExcludeFromCodeCoverage]
	public class TestFileByteSource : TestIByteSource<FileByteSource> {
		private static string GetTestDataPath(int length) {
			return string.Format("[Test Data]/{0}byte.dcm", length);
		}

		private static bool HasTestData(int length) {
			return File.Exists(GetTestDataPath(length));
		}

		[TestMethod]
		public void TestDoubleDispose() {
			var source = CreateByteSource(256);
			source.Dispose();
			source.Dispose();
		}

		protected override FileByteSource CreateByteSource(int length) {
			if (!HasTestData(length))
				Assert.Inconclusive("No test data file {0} available.", GetTestDataPath(length));
			return new FileByteSource(new FileReference(GetTestDataPath(length)));
		}

		protected override string GetSourceName() {
			return "file";
		}

		protected override void ReleaseByteSource(FileByteSource source) {
			source.Dispose();
		}
	}
}