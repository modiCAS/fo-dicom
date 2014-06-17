﻿using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading;

using Dicom.IO;
using Dicom.IO.Buffer;

using Dicom.Imaging.Mathematics;

namespace Dicom.IO.Reader {
	public class DicomReader : IDicomReader {
		private const uint UndefinedLength = 0xffffffff;

		public enum ParseState {
			Tag,
			VR,
			Length,
			Value
		}

		private ParseState _state;
		private DicomTag _tag;
		private DicomDictionaryEntry _entry;
		private DicomVR _vr;
		private uint _length;
		private bool _implicit;

		private int _fragmentItem;

		private DicomTag _stop;
		private IDicomReaderObserver _observer;
		private EventAsyncResult _async;
		private Exception _exception;
		private volatile DicomReaderResult _result;

		private bool _explicit;
		private bool _badPrivateSequence;

		private DicomDictionary _dict;

		private Dictionary<uint, string> _private;
		private Stack<object> _stack;

		public DicomReader() {
			_private = new Dictionary<uint, string>();
			_stack = new Stack<object>();
			_dict = DicomDictionary.Default;
		}

		public DicomDictionary Dictionary {
			get { return _dict; }
			set { _dict = value; }
		}

		public bool IsExplicitVR {
			get { return _explicit; }
			set { _explicit = value; }
		}

		public DicomReaderResult Status {
			get { return _result; }
		}

		public DicomReaderResult Read(IByteSource source, IDicomReaderObserver observer, DicomTag stop = null) {
			return EndRead(BeginRead(source, observer, stop, null, null));
		}

		public IAsyncResult BeginRead(IByteSource source, IDicomReaderObserver observer, DicomTag stop, AsyncCallback callback, object state) {
			_stop = stop;
			_observer = observer;
			_result = DicomReaderResult.Processing;
			_exception = null;
			_async = new EventAsyncResult(callback, state);
			ThreadPool.QueueUserWorkItem(ParseProc, source);
			return _async;
		}

		public DicomReaderResult EndRead(IAsyncResult result) {
			_async.AsyncWaitHandle.WaitOne();
			if (_exception != null)
				throw _exception;
			return _result;
		}

		private void ParseProc(object state) {
			ParseDataset(state as IByteSource, null);
		}

		private ReaderResult ParseDataset(IByteSource source, object state) {
			try {
				return ParseDatasetProc(source, state);
			} catch (Exception e) {
				_exception = e;
				_result = DicomReaderResult.Error;
				return ReaderResult.Failure(e);
			} finally {
				if (_result != DicomReaderResult.Processing && _result != DicomReaderResult.Suspended) {
					_async.Set();
				}
			}
		}

		private ReaderResult ParseDatasetProc(IByteSource source, object state) {
			_result = DicomReaderResult.Processing;

			while (!source.IsEOF && !source.HasReachedMilestone() && _result == DicomReaderResult.Processing) {
				if (_state == ParseState.Tag) {
					source.Mark();

					ReaderResult result = source.Require(4, ParseDataset, state);
					if (!result.IsSuccess) {
						_result = result;
						return result;
					}

					ushort group = source.GetUInt16();
					ushort element = source.GetUInt16();
					DicomPrivateCreator creator = null;

					if (group.IsOdd() && element > 0x00ff) {
						string pvt = null;
						uint card = (uint)(group << 16) + (uint)(element >> 8);
						if (_private.TryGetValue(card, out pvt))
							creator = Dictionary.GetPrivateCreator(pvt);
					}

					_tag = new DicomTag(group, element, creator);
					_entry = Dictionary[_tag];

					if (!_tag.IsPrivate && _entry != null && _entry.MaskTag == null)
						_tag = _entry.Tag; // Use dictionary tag

					if (_stop != null && _tag.CompareTo(_stop) >= 0) {
						_result = DicomReaderResult.Stopped;
						return ReaderResult.Stopped();
					}

					_state = ParseState.VR;
				}

				while (_state == ParseState.VR) {
					if (_tag == DicomTag.Item || _tag == DicomTag.ItemDelimitationItem || _tag == DicomTag.SequenceDelimitationItem) {
						_vr = DicomVR.NONE;
						_state = ParseState.Length;
						break;
					}

					if (IsExplicitVR) {
						ReaderResult result = source.Require(2, ParseDataset, state);
						if ( !result.IsSuccess) {
							_result = result;
							return result;
						}

						byte[] bytes = source.GetBytes(2);
						string vr = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
						if (!DicomVR.TryParse(vr, out _vr)) {
							// unable to parse VR
							_vr = DicomVR.UN;
						}
					} else {
						if (_entry != null) {
							if (_entry == DicomDictionary.UnknownTag)
								_vr = DicomVR.UN;
							else if (_entry.ValueRepresentations.Contains(DicomVR.OB) && _entry.ValueRepresentations.Contains(DicomVR.OW))
								_vr = DicomVR.OW; // ???
							else
								_vr = _entry.ValueRepresentations.FirstOrDefault();
						}
					}

					if (_vr == null)
						_vr = DicomVR.UN;

					_state = ParseState.Length;

					if (_vr == DicomVR.UN) {
						if (_tag.Element == 0x0000) {
							// Group Length to UL
							_vr = DicomVR.UL;
							break;
						} else if (IsExplicitVR) {
							break;
						}
					}

					if (_tag.IsPrivate) {
						if (_tag.Element != 0x0000 && _tag.Element <= 0x00ff && _vr == DicomVR.UN)
							_vr = DicomVR.LO; // force private creator to LO
					}
				}

				while (_state == ParseState.Length) {
					if (_tag == DicomTag.Item || _tag == DicomTag.ItemDelimitationItem || _tag == DicomTag.SequenceDelimitationItem) {
						ReaderResult result = source.Require(4, ParseDataset, state);
						if (!result.IsSuccess) {
							_result = result;
							return result;
						}

						_length = source.GetUInt32();

						_state = ParseState.Value;
						break;
					}

					if (IsExplicitVR) {
						if (_vr.Is16bitLength) {
							ReaderResult result = source.Require(2, ParseDataset, state);
							if (!result.IsSuccess) {
								_result = result;
								return result;
							}

							_length = source.GetUInt16();
						} else {
							ReaderResult result = source.Require(6, ParseDataset, state);
							if (!result.IsSuccess) {
								_result = result;
								return result;
							}

							source.Skip(2);
							_length = source.GetUInt32();
						}
					} else {
						ReaderResult result = source.Require(4, ParseDataset, state);
						if (!result.IsSuccess) {
							_result = result;
							return result;
						}

						_length = source.GetUInt32();

						// assume that undefined length in implicit dataset is SQ
						if (_length == UndefinedLength && _vr == DicomVR.UN)
							_vr = DicomVR.SQ;
					}

					_state = ParseState.Value;
				}

				if (_state == ParseState.Value) {
					// check dictionary for VR after reading length to handle 16-bit lengths
					// check before reading value to handle SQ elements
					if (_vr == DicomVR.UN && IsExplicitVR) {
						var entry = Dictionary[_tag];
						if (entry != null)
							_vr = entry.ValueRepresentations.FirstOrDefault();

						if (_vr == null)
							_vr = DicomVR.UN;
					}

					if (_tag == DicomTag.ItemDelimitationItem) {
						// end of sequence item
						return ReaderResult.Success();
					}

					while (_vr == DicomVR.SQ && _tag.IsPrivate) {
						if (!IsPrivateSequence(source)) {
							_vr = DicomVR.UN;
							break;
						}

						if (IsPrivateSequenceBad(source)) {
							_badPrivateSequence = true;
							_explicit = !_explicit;
						}
						break;
					}

					if (_vr == DicomVR.SQ) {
						// start of sequence
						_observer.OnBeginSequence(source, _tag, _length);
						_state = ParseState.Tag;
						if (_length != UndefinedLength) {
							_implicit = false;
							source.PushMilestone(_length);
						} else
							_implicit = true;
						PushState(state);
						ParseItemSequence(source, null);
						continue;
					}

					if (_length == UndefinedLength) {
						_observer.OnBeginFragmentSequence(source, _tag, _vr);
						_state = ParseState.Tag;
						PushState(state);
						ParseFragmentSequence(source, null);
						continue;
					}

					ReaderResult result = source.Require(_length, ParseDataset, state);
					if (!result.IsSuccess) {
						_result = result;
						return result;
					}

					IByteBuffer buffer = source.GetBuffer(_length);

					if (!_vr.IsString)
						buffer = EndianByteBuffer.Create(buffer, source.Endian, _vr.UnitSize);
					_observer.OnElement(source, _tag, _vr, buffer);

					// parse private creator value and add to lookup table
					if (_tag.IsPrivate && _tag.Element != 0x0000 && _tag.Element <= 0x00ff) {
						var creator = DicomEncoding.Default.GetString(buffer.Data, 0, buffer.Data.Length).TrimEnd((char)DicomVR.LO.PaddingValue);
						var card = (uint)(_tag.Group << 16) + (uint)(_tag.Element);
						_private[card] = creator;
					}

					ResetState();
				}
			}

			if (source.HasReachedMilestone()) {
				// end of explicit length sequence item
				source.PopMilestone();
				return ReaderResult.Success();
			}

			if (_result != DicomReaderResult.Processing)
				return _result;

			// end of processing
			_result = DicomReaderResult.Success;
			return ReaderResult.Success();
		}

		private ReaderResult ParseItemSequence(IByteSource source, object state) {
			try {
				return ParseItemSequenceProc(source, state);
			} catch (Exception e) {
				_exception = e;
				_result = DicomReaderResult.Error;
				return ReaderResult.Failure(e);
			} finally {
				if (_result != DicomReaderResult.Processing && _result != DicomReaderResult.Suspended) {
					_async.Set();
				}
			}
		}

		private ReaderResult ParseItemSequenceProc(IByteSource source, object state) {
			_result = DicomReaderResult.Processing;

			while (!source.IsEOF && !source.HasReachedMilestone()) {
				if (_state == ParseState.Tag) {
					source.Mark();

					ReaderResult result = source.Require(8, ParseItemSequence, state);
					if (!result.IsSuccess) {
						_result = result;
						return result;
					}

					ushort group = source.GetUInt16();
					ushort element = source.GetUInt16();

					_tag = new DicomTag(group, element);

					if (_tag != DicomTag.Item && _tag != DicomTag.SequenceDelimitationItem) {
						// assume invalid sequence
						source.Rewind();
						if (!_implicit)
							source.PopMilestone();
						_observer.OnEndSequence();
						if (_badPrivateSequence) {
							_explicit = !_explicit;
							_badPrivateSequence = false;
						}
						return ReaderResult.Success();
					}

					_length = source.GetUInt32();

					if (_tag == DicomTag.SequenceDelimitationItem) {
						// end of sequence
						_observer.OnEndSequence();
						if (_badPrivateSequence) {
							_explicit = !_explicit;
							_badPrivateSequence = false;
						}
						ResetState();
						return ReaderResult.Success();
					}

					_state = ParseState.Value;
				}

				if (_state == ParseState.Value) {
					if (_length != UndefinedLength) {
						ReaderResult result = source.Require(_length, ParseItemSequence, state);
						if (!result.IsSuccess) {
							_result = result;
							return result;
						}

						source.PushMilestone(_length);
					}

					_observer.OnBeginSequenceItem(source, _length);

					ResetState();
					ParseDataset(source, state);
					ResetState();

					_observer.OnEndSequenceItem();
					continue;
				}
			}

			// end of explicit length sequence
			if (source.HasReachedMilestone())
				source.PopMilestone();

			_observer.OnEndSequence();
			if (_badPrivateSequence) {
				_explicit = !_explicit;
				_badPrivateSequence = false;
			}

			return ReaderResult.Success();
		}

		private bool IsPrivateSequence(IByteSource source) {
			source.Mark();

			try {
				var group = source.GetUInt16();
				var element = source.GetUInt16();
				var tag = new DicomTag(group, element);

				if (tag == DicomTag.Item || tag == DicomTag.SequenceDelimitationItem)
					return true;
			} finally {
				source.Rewind();
			}

			return false;
		}

		private bool IsPrivateSequenceBad(IByteSource source) {
			source.Mark();

			try {
				var group = source.GetUInt16();
				var element = source.GetUInt16();
				var tag = new DicomTag(group, element);
				var length = source.GetUInt32();

				group = source.GetUInt16();
				element = source.GetUInt16();
				tag = new DicomTag(group, element);

				byte[] bytes = source.GetBytes(2);
				string vr = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
				DicomVR dummy;
				if (DicomVR.TryParse(vr,out dummy))
					return !_explicit;
				// unable to parse VR
				if (_explicit)
					return true;
			} finally {
				source.Rewind();
			}

			return false;
		}

		private ReaderResult ParseFragmentSequence(IByteSource source, object state) {
			try {
				return ParseFragmentSequenceProc(source, state);
			} catch (Exception e) {
				_exception = e;
				_result = DicomReaderResult.Error;
				return ReaderResult.Failure(e);
			} finally {
				if (_result != DicomReaderResult.Processing && _result != DicomReaderResult.Suspended) {
					_async.Set();
				}
			}
		}

		private ReaderResult ParseFragmentSequenceProc(IByteSource source, object state) {
			_result = DicomReaderResult.Processing;

			while (!source.IsEOF) {
				if (_state == ParseState.Tag) {
					source.Mark();

					ReaderResult result = source.Require(8, ParseFragmentSequence, state);
					if (!result.IsSuccess) {
						_result = result;
						return result;
					}

					ushort group = source.GetUInt16();
					ushort element = source.GetUInt16();

					DicomTag tag = new DicomTag(group, element);

					if (tag != DicomTag.Item && tag != DicomTag.SequenceDelimitationItem)
						throw new DicomReaderException("Unexpected tag in DICOM fragment sequence: {0}", tag);

					_length = source.GetUInt32();

					if (tag == DicomTag.SequenceDelimitationItem) {
						// end of fragment
						_observer.OnEndFragmentSequence();
						_fragmentItem = 0;
						ResetState();
						ParseDataset(source, PopState());
						return ReaderResult.Success();
					}

					_fragmentItem++;
					_state = ParseState.Value;
				}

				if (_state == ParseState.Value) {
					ReaderResult result = source.Require(_length, ParseFragmentSequence, state);
					if (!result.IsSuccess) {
						_result = result;
						return result;
					}

					IByteBuffer buffer = source.GetBuffer(_length);
					if (_fragmentItem == 1)
						buffer = EndianByteBuffer.Create(buffer, source.Endian, 4);
					else
						buffer = EndianByteBuffer.Create(buffer, source.Endian, _vr.UnitSize);
					_observer.OnFragmentSequenceItem(source, buffer);

					_state = ParseState.Tag;
				}
			}

			return ReaderResult.Success();
		}

		private void PushState(object state) {
			_stack.Push(state);
		}

		private object PopState() {
			if (_stack.Count > 0)
				return _stack.Pop();
			return null;
		}

		private void ResetState() {
			_state = ParseState.Tag;
			_tag = null;
			_entry = null;
			_vr = null;
			_length = 0;
		}
	}
}
