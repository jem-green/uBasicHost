using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using TracerLibrary;

namespace uBasicLibrary
{
    public class uBasic : IInterpreter, IDisposable
    {
        [DllImport("ubasic.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ubasic_run();

        [DllImport("ubasic.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ubasic_init(byte[] memory, UInt32 size);

        [DllImport("ubasic.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ubasic_reset();

        [DllImport("ubasic.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ubasic_finished();

        [DllImport("ubasic.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ubasic_load_program(byte[] program);

        [DllImport("ubasic.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ubasic_resume();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void CallbackDelegate(string valueStr);

        [DllImport("ubasic.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ubasic_callback(IntPtr cb);

        private CallbackDelegate _callerDelegate;
        private IDefaultIO _defaultIO = null;
        private bool _ended = false;
        private byte[] _program;
        private byte[] _memory = new byte[4096];
        private UInt32 _size = 0;

        // Pause/resume state
        private bool _paused = false;
        private readonly object _pauseLock = new object();
        #region Constructors

        public uBasic(byte[] memory, UInt32 size, IDefaultIO consoleIO)
        {
            _defaultIO = consoleIO;

            // Ensure we have a sufficiently large memory buffer. If caller passed null create default.
            _memory = memory;
            _size = size;
            _ended = false;

            // Assign the delegate to the instance field
            _callerDelegate = new CallbackDelegate(MyCallback);
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(_callerDelegate);
            ubasic_callback(ptr);
        }
        #endregion

        #region Properties

        public byte[] Memory
        {
            get
            {
                return _memory;
            }
            set
            {
                _memory = value;
            }
        }

        public UInt32 Size
        {
            get
            {
                return _size;
            }
            set
            {
                _size = value;
            }
        }

        #endregion
        #region Methods

        public void Init()
        {
            Debug.WriteLine("In uBasic.Init()");
            // Pass the pinned pointer to native init
            ubasic_init(_memory, _size);
            _ended = false;
            Debug.WriteLine("Out uBasic.Init()");
        }

        /// <summary>
        /// Load a program into the interpreter. The program is passed as a byte array and will be
        /// pinned in memory for the duration of the call to prevent GC movement. The native loader
        /// will read the program from this buffer and copy it into its own internal memory structures.
        /// After Load returns, the caller is free to modify or dispose of the original byte array without
        /// affecting the loaded program in the interpreter. This design allows for efficient loading of
        /// programs while ensuring memory safety and preventing issues with garbage collection.
        /// </summary>
        /// <param name="program"></param>
        public void Load(byte[] program)
        {
            Debug.WriteLine("In uBasic.Load()");
            _program = program ?? Array.Empty<byte>();

            // Call native loader with pinned memory pointer. program array will be pinned for duration of the call by the marshaller.
            ubasic_load_program(_program);
            _ended = false;
            Debug.WriteLine("Out uBasic.Load()");
        }

        public void Reset()
        {
            Debug.WriteLine("In uBasic.Reset()");
            ubasic_reset();
            _ended = false;
            Debug.WriteLine("Out uBasic.Reset()");
        }

        public void Run()
        {
            Debug.WriteLine("In uBasic.Run()");
            try
            {
                ubasic_run();
            }
            catch (Exception ex)
            {
                TraceInternal.TraceError("Run exception: " + ex.Message);
                throw;
            }
           Debug.WriteLine("Out uBasic.Run()");
        }

        public void Stop()
        {
            Debug.WriteLine("In uBasic.Stop()");
            _ended = true;
            Debug.WriteLine("Out uBasic.Stop()");
        }

        public void Resume()
        {
            Debug.WriteLine("In uBasic.Resume()");
            _ended = false;
            ubasic_resume();
            Debug.WriteLine("Out uBasic.Resume()");
        }

        public bool IsFinished()
        {
            Debug.WriteLine("In uBasic.IsFinished()");
            bool finished = false;
            if ((ubasic_finished() != 0) || _ended)
            {
                TraceInternal.TraceVerbose("Program has finished");
                finished = true;
            }
            Debug.WriteLine("Out uBasic.IsFinished()");
            return (finished);
        }

        public void Abort(string text)
        {
            Debug.WriteLine("In uBasic.Abort()");
            string message = text + " @ "; // + currentLineNumber;
            throw new Exception(message);
        }

        private void MyCallback(string valueStr)
        {
            _defaultIO.Out(valueStr);
        }

        // IDisposable to free pinned handle
        public void Dispose()
        {
            _memory = null;
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
