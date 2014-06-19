using System.Diagnostics.CodeAnalysis;
using Dicom.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DICOM__Unit_Tests_.IO {
	/// <summary>
	/// Generic base unit test class for all IByteSource implementations.
	/// </summary>
	/// <typeparam name="T">IByteSource implementation to be tested.</typeparam>
	[TestClass, ExcludeFromCodeCoverage]
	public abstract class TestIByteSource<T> where T : class, IByteSource {
		/// <summary>
		/// Reference to the source instance created during a test.
		/// </summary>
		/// <remarks>
		/// The source reference is stored for the case that it should be released / disposed
		/// after test execution.
		/// </remarks>
		/// <seealso cref="CleanUp"/>
		private T _source;

		/// <summary>
		/// Release byte source allocated during last test.
		/// </summary>
		[TestCleanup]
		public void CleanUp() {
			if (_source != null) ReleaseByteSource(_source);
			_source = null;
		}

		/// <summary>
		/// Test default properties of a byte source.
		/// </summary>
		[TestMethod]
		public void TestDefaults() {
			_source = CreateByteSource(256);
			Assert.AreEqual(Endian.LocalMachine, _source.Endian);
			Assert.AreEqual(0, _source.Marker);
			Assert.AreEqual(0, _source.Position);
			Assert.IsFalse(_source.IsEOF);
			Assert.IsTrue(_source.CanRewind);
		}

		/// <summary>
		/// Test what happens if an available amount of bytes is requested
		/// from a byte source.
		/// </summary>
		[TestMethod]
		public void TestRequireValid() {
			_source = CreateByteSource(256);
			Assert.IsTrue(_source.Require(128));
		}

		/// <summary>
		/// Test what happens if too many bytes were requested from a byte source.
		/// </summary>
		[TestMethod, ExpectedException(typeof(DicomIoException))]
		public void TestRequireInvalid() {
			_source = CreateByteSource(256);
			_source.Require(512);
		}

		/// <summary>
		/// Test that the error returned on a request of too many bytes contains a
		/// correct description message.
		/// </summary>
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

		/// <summary>
		/// Create a new instance of the byte source with the specified amount of bytes.
		/// </summary>
		/// <param name="length">Amount of bytes to be provided by the new byte source.</param>
		/// <returns>New instance of the byte source.</returns>
		/// <seealso cref="ReleaseByteSource(T)"/>
		protected abstract T CreateByteSource(int length);

		/// <summary>
		/// Get a descriptive name of the tested byte source (e.g. 'file', or 'fixed length stream').
		/// </summary>
		/// <returns>Descriptive name of the tested byte source.</returns>
		protected abstract string GetSourceName();

		/// <summary>
		/// Release the byte source instance previously created in CreateBySource(int).
		/// </summary>
		/// <param name="source">Instance of the test source to be released.</param>
		/// <remarks>The default implementation has no effect.</remarks>
		/// <seealso cref="CreateByteSource(int)"/>
		protected virtual void ReleaseByteSource(T source) {
		}
	}
}
