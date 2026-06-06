/*
 * Copyright (c) 2006, Adam Dunkels
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. Neither the name of the author nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR AND CONTRIBUTORS ``AS IS'' AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 * OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
 *
 */
#ifndef __UBASIC_H__
#define __UBASIC_H__

#include <stddef.h>
#include <stdint.h>

#include "vartype.h"

typedef VARIABLE_TYPE (*peek_func)(VARIABLE_TYPE);
typedef void (*poke_func)(VARIABLE_TYPE, VARIABLE_TYPE);

/*
 * Classic 8-bit BASIC memory layout:
 *   LOW ADDRESSES (start of buffer):
 *     int32_t resume_offset at offset 0 (tokenizer position for resume)
 *     int32_t gosub_depth at offset 4
 *     int32_t for_depth at offset 8
 *     int32_t gosub_stack[UBASIC_MAX_GOSUB_STACK_DEPTH]
 *     struct for_state for_stack[UBASIC_MAX_FOR_STACK_DEPTH]
 *     program bytes (NUL-terminated, grows upward)
 *   
 *   HIGH ADDRESSES (top of buffer, when using ubasic_init_sized):
 *     uint8_t heap[UBASIC_HEAP_BYTES] (optional, for future string/double)
 *     VARIABLE_TYPE variables[UBASIC_VARIABLE_COUNT] (AT TOP)
 */
#define UBASIC_MAX_GOSUB_STACK_DEPTH 10
#define UBASIC_MAX_FOR_STACK_DEPTH 4
#define UBASIC_VARIABLE_COUNT 26

#ifndef UBASIC_HEAP_BYTES
#define UBASIC_HEAP_BYTES 0
#endif

typedef struct for_state {
  int32_t line_after_for;
  int32_t for_variable_index;
  int32_t to;
} for_state;

/* Control state at low addresses */
#define UBASIC_MEM_RESUME_OFFSET      0
#define UBASIC_MEM_GOSUB_DEPTH_OFFSET 4
#define UBASIC_MEM_FOR_DEPTH_OFFSET   8
#define UBASIC_MEM_GOSUB_STACK_OFFSET 12
#define UBASIC_MEM_FOR_STACK_OFFSET \
  (UBASIC_MEM_GOSUB_STACK_OFFSET + UBASIC_MAX_GOSUB_STACK_DEPTH * (int)sizeof(int32_t))
#define UBASIC_MEM_PROGRAM_OFFSET \
  (UBASIC_MEM_FOR_STACK_OFFSET + UBASIC_MAX_FOR_STACK_DEPTH * (int)sizeof(for_state))

/* Variables at top (calculated at runtime) */
#define UBASIC_VARIABLES_SIZE (UBASIC_VARIABLE_COUNT * (int)sizeof(VARIABLE_TYPE))
#define UBASIC_MIN_MEMORY_BYTES (UBASIC_MEM_PROGRAM_OFFSET + 1u + UBASIC_HEAP_BYTES + UBASIC_VARIABLES_SIZE)

// Public

__declspec(dllexport) void ubasic_init(uint8_t *memory, uint32_t memory_bytes);  /* Vars at top */
__declspec(dllexport) void ubasic_reset(void);  /* Clears variables */
__declspec(dllexport) void ubasic_run(void);
__declspec(dllexport) int ubasic_finished(void);
__declspec(dllexport) void ubasic_load_program(const char *program);  /* Preserves vars */
__declspec(dllexport) void ubasic_resume(void);  /* Resume from saved position in memory */

// Callback

typedef void (*Callback)(const char* value);
__declspec(dllexport) void ubasic_callback(Callback cb);

#endif /* __UBASIC_H__ */
