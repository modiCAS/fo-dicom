using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using Dicom.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DICOM__Unit_Tests_.IO {
	[TestClass, ExcludeFromCodeCoverage]
	public abstract class TestIByteSource<T> where T : class, IByteSource {
		private T _source;

		[TestCleanup]
		public void CleanUp() {
			if (_source != null) ReleaseByteSource(_source);
			_source = null;
		}

		[TestMethod]
		public void TestDefaults() {
			_source = CreateByteSource(256);
			Assert.AreEqual(Endian.LocalMachine, _source.Endian);
			Assert.AreEqual(0,_source.Marker);
			Assert.AreEqual(0,_source.Position);
			Assert.IsFalse(_source.IsEOF);
			Assert.IsTrue(_source.CanRewind);
		}

		[TestMethod]
		public void TestRequireValid() {
			_source = CreateByteSource(256);
			Assert.IsTrue(_source.Require(128));
		}

		[TestMethod, ExpectedException(typeof(DicomIoException))]
		public void TestRequireInvalid() {
			_source = CreateByteSource(256);
			_source.Require(512);
		}

		[TestMethod]
		public void TestRequireInvalidExceptionText() {
			_source = CreateByteSource(256);
			try {
				_source.Require(512);
			} catch (DicomIoException e) {
				Assert.AreEqual(string.Format("Requested {0} bytes past end of {1}.",
					512, GetSourceName()), e.Message);
			}
		}

		protected abstract T CreateByteSource(int length);

		protected abstract string GetSourceName();

		protected virtual void ReleaseByteSource(T source) {
		}
	}
}
