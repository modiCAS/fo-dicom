using System;
using Dicom.IO.Reader;

namespace Dicom.IO {
	public class ReaderResult {
		private readonly DicomReaderResult _result;
		private readonly Exception _exception;
		private readonly string _message;

		private ReaderResult(DicomReaderResult result) {
			_result = result;
			_exception = null;
			_message = result.ToString();
		}

		private ReaderResult(DicomReaderResult result, string message) {
			_result = result;
			_exception = null;
			_message = message;
		}

		private ReaderResult(DicomReaderResult result, Exception exception) {
			_result = result;
			_exception = exception;
			_message = exception.Message;
		}

		public Exception Exception {
			get { return _exception; }
		}

		public bool IsBusy {
			get { return _result == DicomReaderResult.Processing || _result == DicomReaderResult.Suspended; }
		}

		public bool IsSuccess {
			get { return _result == DicomReaderResult.Success; }
		}

		public string Message {
			get { return _message; }
		}

		public static ReaderResult Failure(string format, params object[] args) {
			return new ReaderResult(DicomReaderResult.Error, string.Format(format, args));
		}

		public static ReaderResult Failure(Exception exception) {
			return new ReaderResult(DicomReaderResult.Error, exception);
		}

		public static ReaderResult Stopped() {
			return new ReaderResult(DicomReaderResult.Stopped);
		}

		public static ReaderResult Success() {
			return new ReaderResult(DicomReaderResult.Success);
		}

		public static ReaderResult Suspend() {
			return new ReaderResult(DicomReaderResult.Suspended);
		}

		public static implicit operator DicomReaderResult(ReaderResult result) {
			return result._result;
		}

		public static implicit operator ReaderResult(DicomReaderResult result) {
			return new ReaderResult(result);
		}

		public static ReaderResult Processing() {
			return new ReaderResult(DicomReaderResult.Processing);
		}
	}
}