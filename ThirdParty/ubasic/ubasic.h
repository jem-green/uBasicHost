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

/* I/O function pointers matching C# wrapper API */
typedef void (*out_func)(const char *message);
typedef void (*put_func)(char character);
typedef void (*in_func)(char *buffer, size_t max_len);
typedef char (*get_func)(void);
typedef void (*err_func)(const char *code, const char *description, uint32_t line);

/*
 * Classic 8-bit BASIC memory layout:
 *   LOW MEMORY (start of buffer):
 *     int32_t resume_offset at offset 0 (tokenizer position for resume)
 *     int32_t gosub_depth at offset 4
 *     int32_t for_depth at offset 8
 *     int32_t gosub_stack[UBASIC_MAX_GOSUB_STACK_DEPTH]
 *     struct for_state for_stack[UBASIC_MAX_FOR_STACK_DEPTH]
 *     program bytes (NUL-terminated)
 *     VARIABLE_TYPE variables[UBASIC_VARIABLE_COUNT] (after program)
 *     uint8_t heap[UBASIC_HEAP_BYTES] (optional, for future string/double)
 *   HIGH MEMORY (free space)
 */
#define UBASIC_MAX_GOSUB_STACK_DEPTH 10
#define UBASIC_MAX_FOR_STACK_DEPTH 4
#define UBASIC_MAX_PUSH_STACK_DEPTH 16
#define UBASIC_VARIABLE_COUNT 26

#ifndef UBASIC_HEAP_BYTES
#define UBASIC_HEAP_BYTES 0
#endif

typedef struct for_state {
  uint32_t line_after_for;
  int32_t for_variable_index; /* 0..25 (a-z) */
  int32_t to;
} for_state;

/* Control state at low addresses */
#define UBASIC_MEM_RESUME_OFFSET      0
#define UBASIC_MEM_GOSUB_DEPTH_OFFSET 4
#define UBASIC_MEM_FOR_DEPTH_OFFSET   8
#define UBASIC_MEM_GOSUB_STACK_OFFSET 12
#define UBASIC_MEM_FOR_STACK_OFFSET \
  (UBASIC_MEM_GOSUB_STACK_OFFSET + UBASIC_MAX_GOSUB_STACK_DEPTH * (int)sizeof(int32_t))
/* PUSH/POP stack: depth cell + int32_t slots */
#define UBASIC_MEM_PUSH_DEPTH_OFFSET \
  (UBASIC_MEM_FOR_STACK_OFFSET + UBASIC_MAX_FOR_STACK_DEPTH * (int)sizeof(for_state))
#define UBASIC_MEM_PUSH_STACK_OFFSET \
  (UBASIC_MEM_PUSH_DEPTH_OFFSET + (int)sizeof(int32_t))
/* Program starts immediately after control structures */
#define UBASIC_MEM_PROGRAM_OFFSET \
  (UBASIC_MEM_PUSH_STACK_OFFSET + UBASIC_MAX_PUSH_STACK_DEPTH * (int)sizeof(int32_t))

/* Variables placed in LOW memory after program (calculated at runtime) */
#define UBASIC_VARIABLES_SIZE (UBASIC_VARIABLE_COUNT * (int)sizeof(VARIABLE_TYPE))
#define UBASIC_PUSH_STACK_SIZE (UBASIC_MAX_PUSH_STACK_DEPTH * (int)sizeof(int32_t))
#define UBASIC_MIN_MEMORY_BYTES (UBASIC_MEM_PROGRAM_OFFSET + 1u + UBASIC_VARIABLES_SIZE + UBASIC_HEAP_BYTES)

// Public

void ubasic_init(uint8_t *memory, uint32_t memory_bytes);  /* Minimal init for snapshot restore */
void ubasic_reset(void);  /* Clears variables */
void ubasic_run(void);
int ubasic_finished(void);
void ubasic_load_program(const char *program); /* Preserves vars */


/* I/O function setters matching C# wrapper API */
void ubasic_set_out_function(out_func func);
void ubasic_set_put_function(put_func func);
void ubasic_set_in_function(in_func func);
void ubasic_set_get_function(get_func func);
void ubasic_set_error_function(err_func func);
const char *ubasic_last_error_code(void);
const char *ubasic_last_error_description(void);
uint32_t ubasic_last_error_line(void);

#endif /* __UBASIC_H__ */
