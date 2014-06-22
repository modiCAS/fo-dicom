using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace DICOM__Unit_Tests_ {
	/// <summary>
	/// Unit test data utilities.
	/// </summary>
	[ExcludeFromCodeCoverage]
	public static class TestData {
		/// <summary>
		/// Compute a test data path based on specified byte length.
		/// </summary>
		/// <param name="length">Length of the requested file.</param>
		/// <returns>File name to a test data file of requested length.</returns>
		public static string GetPath(int length) {
			return string.Format("[Test Data]/{0}byte.dcm", length);
		}

		/// <summary>
		/// Check if a test data file of specified length is available.
		/// </summary>
		/// <param name="length">Length of the requested file.</param>
		/// <returns>
		///	true if a test data file of requested length is available,
		/// false otherwise.
		/// </returns>
		public static bool IsAvailable(int length) {
			return File.Exists(GetPath(length));
		}
	}
}