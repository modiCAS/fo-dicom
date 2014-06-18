using System.Diagnostics.CodeAnalysis;
using System.IO;
using Dicom.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DICOM__Unit_Tests_.IO {
	[TestClass, ExcludeFromCodeCoverage]
	public class TestStreamByteSource : TestIByteSource<StreamByteSource> {
		protected override StreamByteSource CreateByteSource(int length) {
			return new StreamByteSource(new MemoryStream(new byte[length]));
		}

		protected override string GetSourceName() {
			return "fixed length stream";
		}
	}
}