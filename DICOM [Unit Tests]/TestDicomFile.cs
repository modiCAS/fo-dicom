using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Dicom;
using Dicom.Imaging.LUT;
using Dicom.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DICOM__Unit_Tests_ {
	/// <summary>
	/// Unit tests for the dicom file.
	/// </summary>
	[TestClass, ExcludeFromCodeCoverage]
	public class TestDicomFile {
		private static DicomDataset GetEmptyDataset() {
			var dataset = new DicomDataset();
			dataset.Add(DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage);
			dataset.Add(DicomTag.SOPInstanceUID, DicomUID.Generate());
			return dataset;
		}

		/// <summary>
		/// Test values of default properties.
		/// </summary>
		[TestMethod]
		public void TestDefaultProperties() {
			var file = new DicomFile();
			Assert.IsNotNull(file);
			Assert.AreEqual(DicomFileFormat.DICOM3, file.Format);
			Assert.IsNotNull(file.Dataset);
			Assert.IsNotNull(file.FileMetaInfo);
			Assert.IsNull(file.File);
		}

		/// <summary>
		/// Test if the ToString method will produce the expected string.
		/// </summary>
		[TestMethod]
		public void TestToString() {
			var file = new DicomFile();
			Assert.AreEqual("DICOM File [DICOM3]", file.ToString());
		}

		/// <summary>
		/// Test if the file will store to a custom dataset provided in the constructor.
		/// </summary>
		[TestMethod]
		public void TestOwnDataset() {
			DicomDataset dataset = GetEmptyDataset();
			var file = new DicomFile(dataset);
			Assert.AreEqual(dataset, file.Dataset);
		}

		/// <summary>
		/// Test save method with an empty dataset.
		/// </summary>
		[TestMethod]
		public void TestSaveEmpty() {
			var file = new DicomFile(GetEmptyDataset());
			file.Save("empty.dcm");
		}

		/// <summary>
		/// Test save method with a dataset containing study and series info.
		/// </summary>
		[TestMethod]
		public void TestSaveSeries() {
			var dataset = GetEmptyDataset();
			dataset.Add(DicomTag.StudyInstanceUID, DicomUID.Generate());
			dataset.Add(DicomTag.Modality, "OT");
			dataset.Add(DicomTag.SeriesInstanceUID, DicomUID.Generate());
			var file = new DicomFile(dataset);
			file.Save("series.dcm");
		}

		/// <summary>
		/// Test open method on a valid DICOM file.
		/// </summary>
		[TestMethod]
		public void TestOpenValid() {
			DicomFile file = DicomFile.Open("[Test Data]/empty.dcm");
			Assert.IsNotNull(file);
		}

		/// <summary>
		/// Test open method on a DICOM file without preamble.
		/// </summary>
		[TestMethod]
		public void TestOpenNoPreamble() {
			DicomFile file = DicomFile.Open("[Test Data]/nopreamble.dcm");
			Assert.IsNotNull(file);
		}

		/// <summary>
		/// Test open method on a DICOM file without meta information.
		/// </summary>
		[TestMethod]
		public void TestOpenNoMetainfo() {
			DicomFile file = DicomFile.Open("[Test Data]/nometainfo.dcm");
			Assert.IsNotNull(file);
		}

		/// <summary>
		/// Test open method on a very short DICOM file without meta information.
		/// </summary>
		[TestMethod, ExpectedException(typeof(DicomFileException))]
		public void TestOpenNoMetainfoMin() {
			DicomFile file = DicomFile.Open("[Test Data]/nometainfomin.dcm");
			Assert.IsNotNull(file);
		}

		/// <summary>
		/// Test open method on an invalid ACR/NEMA-like file with invalid recognition code.
		/// </summary>
		[TestMethod]
		public void TestOpenAcrNema0() {
			DicomFile file = DicomFile.Open("[Test Data]/acrnema0.dcm");
			Assert.IsNotNull(file);
		}

		/// <summary>
		/// Test open method on a ACR/NEMA 1.0 file.
		/// </summary>
		[TestMethod]
		public void TestOpenAcrNema1() {
			DicomFile file = DicomFile.Open("[Test Data]/acrnema1.dcm");
			Assert.IsNotNull(file);
		}

		/// <summary>
		/// Test open method on a ACR/NEMA 2.0 file.
		/// </summary>
		[TestMethod]
		public void TestOpenAcrNema2() {
			DicomFile file = DicomFile.Open("[Test Data]/acrnema2.dcm");
			Assert.IsNotNull(file);
		}

		/// <summary>
		/// Test open method on an invalid DICOM file.
		/// </summary>
		[TestMethod, ExpectedException(typeof(DicomFileException))]
		public void TestOpenInvalid() {
			DicomFile file = DicomFile.Open(TestData.GetPath(256));
			Assert.Fail("Loading of {0} should have failed", file);
		}

		/// <summary>
		/// Test open method on a valid DICOM file.
		/// </summary>
		[TestMethod]
		public void TestOpenValidStream() {
			DicomFile file = DicomFile.Open(File.OpenRead("[Test Data]/empty.dcm"));
			Assert.IsNotNull(file);
		}

		/// <summary>
		/// Test open method on an invalid DICOM file.
		/// </summary>
		[TestMethod, ExpectedException(typeof(DicomFileException))]
		public void TestOpenInvalidStream() {
			DicomFile file = DicomFile.Open(File.OpenRead(TestData.GetPath(256)));
			Assert.Fail("Loading of {0} should have failed", file);
		}
	}
}
