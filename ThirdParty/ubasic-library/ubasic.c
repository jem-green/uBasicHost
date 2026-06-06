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

#define DEBUG 0
#define VERBOSE 0

#if DEBUG
#define DEBUG_PRINTF(...)  printf(__VA_ARGS__)
#else
#define DEBUG_PRINTF(...)
#endif

#include "ubasic.h"
#include "tokenizer.h"

#include <stdio.h> /* printf() */
#include <stdlib.h> /* exit() */
#include <string.h> /* strlen(), memcpy(), sprintf() */
#include <stdint.h>

// Probably private

typedef VARIABLE_TYPE (*peek_func)(VARIABLE_TYPE);
typedef void (*poke_func)(VARIABLE_TYPE, VARIABLE_TYPE);
static VARIABLE_TYPE expr(void);
static void line_statement(void);
static void statement(void);
static void index_free(void);
static void reset_control_state(void);
static VARIABLE_TYPE expr(void);
void set_variable(int varum, VARIABLE_TYPE value);
VARIABLE_TYPE get_variable(int varnum);
static void gosub_push(int32_t line_num);
static int32_t gosub_pop(void);
static void for_push(for_state state);
static for_state for_pop(void);

static char const *program_ptr;
#define MAX_STRINGLEN 40
static char string[MAX_STRINGLEN];

static uint8_t *mem_base = NULL;
static size_t mem_capacity_bytes = 0;
static int32_t *resume_offset_cell = NULL;
static int32_t *gosub_depth_cell = NULL;
static int32_t *for_depth_cell = NULL;
static int32_t *gosub_stack_mem = NULL;
static for_state *for_stack = NULL;
static VARIABLE_TYPE *variables_mem = NULL;

struct line_index {
  int line_number;
  char const *program_text_position;
  struct line_index *next;
};

struct line_index *line_index_head = NULL;
struct line_index *line_index_current = NULL;

static int ended;

peek_func peek_function = NULL;
poke_func poke_function = NULL;

// Callback

static Callback stored_callback = 0;

/*
 * Buffer passed to ubasic_init (see UBASIC_*_OFFSET in ubasic.h):
 *   gosub depth, for depth, gosub stack[], for stack[],
 *   then NUL-terminated program.
 */




/*---------------------------------------------------------------------------*/
//Public functions
/*---------------------------------------------------------------------------*/
void emit(const char* value) {
  //
  DEBUG_PRINTF(value);
  if (stored_callback) {
      stored_callback(value);
  }
}
/*---------------------------------------------------------------------------*/
__declspec(dllexport) void ubasic_init(uint8_t *memory, uint32_t memory_bytes) {
  size_t variables_offset;
  
  mem_base = memory;
  mem_capacity_bytes = memory_bytes;
  resume_offset_cell = (int32_t *)(memory + UBASIC_MEM_RESUME_OFFSET);
  gosub_depth_cell = (int32_t *)(memory + UBASIC_MEM_GOSUB_DEPTH_OFFSET);
  for_depth_cell = (int32_t *)(memory + UBASIC_MEM_FOR_DEPTH_OFFSET);
  gosub_stack_mem = (int32_t *)(memory + UBASIC_MEM_GOSUB_STACK_OFFSET);
  for_stack = (for_state *)(memory + UBASIC_MEM_FOR_STACK_OFFSET);
  
  /* Variables at TOP of memory */
  variables_offset = memory_bytes - UBASIC_VARIABLES_SIZE - UBASIC_HEAP_BYTES;
  variables_mem = (VARIABLE_TYPE *)(memory + variables_offset);
  
  program_ptr = (char const *)(memory + UBASIC_MEM_PROGRAM_OFFSET);
  index_free();
  tokenizer_init(program_ptr);
  #if VERBOSE
    DEBUG_PRINTF("ubasic_init: Variables at offset %zu (top of memory).\n", variables_offset);
  #endif
}
/*---------------------------------------------------------------------------*/
void ubasic_init_peek_poke(uint8_t *memory, uint32_t memory_bytes,peek_func peek, poke_func poke) {
  ubasic_init(memory, memory_bytes);
  peek_function = peek;
  poke_function = poke;
  #if VERBOSE
    DEBUG_PRINTF("ubasic_init_peek_poke: Initializing uBasic with peek/poke.\n");
  #endif
}
/*---------------------------------------------------------------------------*/
static void reset_control_state(void) {
  index_free();
  tokenizer_reset();
  if (resume_offset_cell != NULL) {
    *resume_offset_cell = 0;
  }
  if (gosub_depth_cell != NULL) {
    *gosub_depth_cell = 0;
  }
  if (for_depth_cell != NULL) {
    *for_depth_cell = 0;
  }
  if (gosub_stack_mem != NULL) {
    memset(gosub_stack_mem, 0, UBASIC_MAX_GOSUB_STACK_DEPTH * sizeof(int32_t));
  }
  if (for_stack != NULL) {
    memset(for_stack, 0, UBASIC_MAX_FOR_STACK_DEPTH * sizeof(for_state));
  }
  ended = 0;
}
/*---------------------------------------------------------------------------*/
__declspec(dllexport) void ubasic_reset(void) {
  reset_control_state();
  /* Also clear variables on full reset */
  if (variables_mem != NULL) {
    memset(variables_mem, 0, UBASIC_VARIABLE_COUNT * sizeof(VARIABLE_TYPE));
  }
  #if VERBOSE
    DEBUG_PRINTF("ubasic_reset: Full reset including variables.\n");
  #endif
}
/*---------------------------------------------------------------------------*/
__declspec(dllexport) void ubasic_load_program(const char *program) {
  size_t len;
  size_t available_program_bytes;

  if (mem_base == NULL) {
    DEBUG_PRINTF("ubasic_load_program: call ubasic_init first.\n");
    return;
  }
  
  /* Reset control state but PRESERVE VARIABLES */
  reset_control_state();
  
  len = strlen(program);
  
  if (mem_capacity_bytes != 0) {
    /* Check program fits between PROGRAM_OFFSET and variables */
    size_t variables_offset = mem_capacity_bytes - UBASIC_VARIABLES_SIZE - UBASIC_HEAP_BYTES;
    if (UBASIC_MEM_PROGRAM_OFFSET + len + 1 > variables_offset) {
      DEBUG_PRINTF("ubasic_load_program: program too large.\n");
      return;
    }
  }
  
  memcpy(mem_base + UBASIC_MEM_PROGRAM_OFFSET, program, len + 1);
  program_ptr = (char const *)(mem_base + UBASIC_MEM_PROGRAM_OFFSET);
  tokenizer_init(program_ptr);
  #if VERBOSE
    DEBUG_PRINTF("ubasic_load_program: Loaded %u bytes, variables preserved.\n", (unsigned)len);
  #endif
}
/*---------------------------------------------------------------------------*/
__declspec(dllexport) int ubasic_finished(void){
  return ended || tokenizer_finished();
}
/*---------------------------------------------------------------------------*/
__declspec(dllexport) void ubasic_run(void){
  if(tokenizer_finished()) {
    #if VERBOSE
      DEBUG_PRINTF("ubasic_run: Program finished.\n");
    #endif
    return;
  }
  line_statement();
}
/*---------------------------------------------------------------------------*/
__declspec(dllexport) void ubasic_callback(Callback cb) {
    stored_callback = cb;
}
/*---------------------------------------------------------------------------*/
static void save_position(void) {
  const char *current_pos = tokenizer_pos();
  const char *start_pos = tokenizer_start();
  int32_t offset = (int32_t)(current_pos - start_pos);
  
  if (resume_offset_cell != NULL) {
    *resume_offset_cell = offset;
    #if VERBOSE
      DEBUG_PRINTF("save_position: Saved offset %d.\n", offset);
    #endif
  }
}
/*---------------------------------------------------------------------------*/
__declspec(dllexport) void ubasic_resume(void) {
  int32_t offset;
  const char *start_pos;
  const char *resume_pos;
  
  if (resume_offset_cell == NULL || program_ptr == NULL) {
    DEBUG_PRINTF("ubasic_resume: Not initialized.\n");
    return;
  }
  
  offset = *resume_offset_cell;
  start_pos = tokenizer_start();
  resume_pos = start_pos + offset;
  
  tokenizer_goto(resume_pos);
  ended = 0;
  
  #if VERBOSE
    DEBUG_PRINTF("ubasic_resume: Resumed from offset %d.\n", offset);
  #endif
}
/*---------------------------------------------------------------------------*/
// Private functions
/*---------------------------------------------------------------------------*/
static void accept(int token){
  if(token != tokenizer_token()) {
    DEBUG_PRINTF("accept: Token not what was expected (expected '%s', got %s).\n",
	tokenizer_token_name(token),
	tokenizer_token_name(tokenizer_token()));
    tokenizer_error_print();
    exit(1);
  }
  DEBUG_PRINTF("accept: Expected '%s', got it.\n", tokenizer_token_name(token));
  tokenizer_next();
}
/*---------------------------------------------------------------------------*/
static int varfactor(void){
  int r;
  DEBUG_PRINTF("varfactor: obtaining %d from variable %d.\n", get_variable(tokenizer_variable_num()), tokenizer_variable_num());
  r = get_variable(tokenizer_variable_num());
  accept(TOKENIZER_VARIABLE);
  return r;
}
/*---------------------------------------------------------------------------*/
static int factor(void){
  int r;

  DEBUG_PRINTF("factor: token '%s'.\n", tokenizer_token_name(tokenizer_token()));
  switch(tokenizer_token()) {
  case TOKENIZER_NUMBER:
    r = tokenizer_num();
    DEBUG_PRINTF("factor: number %d.\n", r);
    accept(TOKENIZER_NUMBER);
    break;
  case TOKENIZER_LEFTPAREN:
    accept(TOKENIZER_LEFTPAREN);
    r = expr();
    accept(TOKENIZER_RIGHTPAREN);
    break;
  default:
    r = varfactor();
    break;
  }
  DEBUG_PRINTF("term: %d.\n", r);
  return r;
}
/*---------------------------------------------------------------------------*/
static int term(void){
  int f1, f2;
  int op;

  f1 = factor();
  op = tokenizer_token();
  DEBUG_PRINTF("term: token '%s'.\n", tokenizer_token_name(op));
  while(op == TOKENIZER_ASTR ||
       op == TOKENIZER_SLASH ||
       op == TOKENIZER_MOD) {
    tokenizer_next();
    f2 = factor();
    DEBUG_PRINTF("term: %d %d %d\n", f1, op, f2);
    switch(op) {
    case TOKENIZER_ASTR:
      f1 = f1 * f2;
      break;
    case TOKENIZER_SLASH:
      f1 = f1 / f2;
      break;
    case TOKENIZER_MOD:
      f1 = f1 % f2;
      break;
    }
    op = tokenizer_token();
  }
  DEBUG_PRINTF("term: factor=%d.\n", f1);
  return f1;
}
/*---------------------------------------------------------------------------*/
static VARIABLE_TYPE expr(void){
  int t1, t2;
  int op;

  t1 = term();
  op = tokenizer_token();
  DEBUG_PRINTF("expr: token %s.\n", tokenizer_token_name(op));
  while(op == TOKENIZER_PLUS ||
       op == TOKENIZER_MINUS ||
       op == TOKENIZER_AND ||
       op == TOKENIZER_OR) {
    tokenizer_next();
    t2 = term();
    DEBUG_PRINTF("expr: %d %d %d.\n", t1, op, t2);
    switch(op) {
    case TOKENIZER_PLUS:
      t1 = t1 + t2;
      break;
    case TOKENIZER_MINUS:
      t1 = t1 - t2;
      break;
    case TOKENIZER_AND:
      t1 = t1 & t2;
      break;
    case TOKENIZER_OR:
      t1 = t1 | t2;
      break;
    }
    op = tokenizer_token();
  }
  DEBUG_PRINTF("expr: term=%d.\n", t1);
  return t1;
}
/*---------------------------------------------------------------------------*/
static int relation(void){
  int r1, r2;
  int op;

  r1 = expr();
  op = tokenizer_token();
  DEBUG_PRINTF("relation: token %d.\n", op);
  while(op == TOKENIZER_LT ||
       op == TOKENIZER_GT ||
       op == TOKENIZER_EQ) {
    tokenizer_next();
    r2 = expr();
    DEBUG_PRINTF("relation: %d %d %d.\n", r1, op, r2);
    switch(op) {
    case TOKENIZER_LT:
      r1 = r1 < r2;
      break;
    case TOKENIZER_GT:
      r1 = r1 > r2;
      break;
    case TOKENIZER_EQ:
      r1 = r1 == r2;
      break;
    }
    op = tokenizer_token();
  }
  DEBUG_PRINTF("relation: expr=%d.\n", r1);
  return r1;
}
/*---------------------------------------------------------------------------*/
static void index_free(void) {
  if(line_index_head != NULL) {
    line_index_current = line_index_head;
    do {
      DEBUG_PRINTF("index_free: Freeing index for line %d.\n", line_index_current->line_number);
      line_index_head = line_index_current;
      line_index_current = line_index_current->next;
      free(line_index_head);
    } while (line_index_current != NULL);
    line_index_head = NULL;
  }
}
/*---------------------------------------------------------------------------*/
static char const* index_find(int linenum) {
  struct line_index *lidx;
  lidx = line_index_head;

  #if DEBUG
  int step = 0;
  #endif

  while(lidx != NULL && lidx->line_number != linenum) {
    lidx = lidx->next;

    #if DEBUG
    	#if VERBOSE
      		if(lidx != NULL) {
        		DEBUG_PRINTF("index_find: Step %3d. Found index for line %d: %p.\n",
        		step,
        		lidx->line_number,
        		lidx->program_text_position - tokenizer_start());
      		}
      		step++;
    	#endif
    #endif
  }
  if(lidx != NULL && lidx->line_number == linenum) {
    #if DEBUG
    	#if VERBOSE
      		DEBUG_PRINTF("index_find: Returning index for line %d.\n", linenum);
    	#endif
    #endif
    return lidx->program_text_position;
  }
  DEBUG_PRINTF("index_find: Returning NULL.\n", linenum);
  return NULL;
}
/*---------------------------------------------------------------------------*/
static void index_add(int linenum, char const* sourcepos) {
  if(line_index_head != NULL && index_find(linenum)) {
    return;
  }

  struct line_index *new_lidx;

  new_lidx = malloc(sizeof(struct line_index));
  new_lidx->line_number = linenum;
  new_lidx->program_text_position = sourcepos;
  new_lidx->next = NULL;

  if(line_index_head != NULL) {
    line_index_current->next = new_lidx;
    line_index_current = line_index_current->next;
  } else {
    line_index_current = new_lidx;
    line_index_head = line_index_current;
  }
  #if DEBUG
  	#if VERBOSE
		DEBUG_PRINTF("index_add: Adding index for line %d: %p.\n", linenum,
			sourcepos - tokenizer_start());
		#endif
	#endif
}
/*---------------------------------------------------------------------------*/
static void jump_linenum_slow(int linenum) {
  tokenizer_init(program_ptr);
  while(tokenizer_num() != linenum) {
    do {
      do {
        tokenizer_next();
      } while(tokenizer_token() != TOKENIZER_LF &&
          tokenizer_token() != TOKENIZER_ENDOFINPUT);
      if(tokenizer_token() == TOKENIZER_LF) {
        tokenizer_next();
      }
    } while(tokenizer_token() != TOKENIZER_NUMBER);
	#if DEBUG
      #if VERBOSE
        DEBUG_PRINTF("jump_linenum_slow: Found line %d.\n", tokenizer_num());
	  #endif
	#endif
  }
}
/*---------------------------------------------------------------------------*/
static void jump_linenum(int linenum) {
  char const* pos = index_find(linenum);
  if(pos != NULL) {
    DEBUG_PRINTF("jump_linenum: Going to line %d.\n", linenum);
    tokenizer_goto(pos);
  } else {
    /* We'll try to find a yet-unindexed line to jump to. */
    DEBUG_PRINTF("jump_linenum: Calling jump_linenum_slow for line %d.\n", linenum);
    jump_linenum_slow(linenum);
  }
}
/*---------------------------------------------------------------------------*/
static void goto_statement(void) {
  accept(TOKENIZER_GOTO);
  DEBUG_PRINTF("jump_linenum: goto.\n");
  jump_linenum(tokenizer_num());
}
/*---------------------------------------------------------------------------*/
static void print_statement(void) {
  static char buf[128];
  buf[0]=0;
  accept(TOKENIZER_PRINT);
  DEBUG_PRINTF("print_statement: Loop.\n");
  do {
    if(tokenizer_token() == TOKENIZER_STRING) {
      tokenizer_string(string, sizeof(string));
	  sprintf(buf+strlen(buf), "%s", string);
      tokenizer_next();
    } else if(tokenizer_token() == TOKENIZER_COMMA) {
      sprintf(buf+strlen(buf), "%s", " ");
      tokenizer_next();
    } else if(tokenizer_token() == TOKENIZER_SEMICOLON) {
      tokenizer_next();
    } else if(tokenizer_token() == TOKENIZER_VARIABLE ||
      tokenizer_token() == TOKENIZER_NUMBER) {
      sprintf(buf+strlen(buf), "%d", expr());
    } else if (tokenizer_token() == TOKENIZER_CR){
      tokenizer_next();
    } else {
      break;
    }
  } while(tokenizer_token() != TOKENIZER_LF &&
      tokenizer_token() != TOKENIZER_ENDOFINPUT);
  emit(buf);
  //printf(buf);
  //printf("\n");
  emit("\n");
  DEBUG_PRINTF("print_statement: End of print.\n");
  tokenizer_next();
}
/*---------------------------------------------------------------------------*/
static void if_statement(void){
  int r;

  accept(TOKENIZER_IF);

  r = relation();
  #if VERBOSE
  	DEBUG_PRINTF("if_statement: relation %d.\n", r);
  #endif
  accept(TOKENIZER_THEN);
  if(r) {
    statement();
  } else {
    do {
      tokenizer_next();
    } while(tokenizer_token() != TOKENIZER_ELSE &&
        tokenizer_token() != TOKENIZER_LF &&
        tokenizer_token() != TOKENIZER_ENDOFINPUT);
    if(tokenizer_token() == TOKENIZER_ELSE) {
      tokenizer_next();
      statement();
    } else if(tokenizer_token() == TOKENIZER_LF) {
      tokenizer_next();
    }
  }
}
/*---------------------------------------------------------------------------*/
static void let_statement(void){
  int var;

  var = tokenizer_variable_num();

  accept(TOKENIZER_VARIABLE);
  accept(TOKENIZER_EQ);
  set_variable(var, expr());
  #if VERBOSE
    DEBUG_PRINTF("let_statement: assign %d to %d.\n", get_variable(var), var);
  #endif
  accept(TOKENIZER_LF);

}
/*---------------------------------------------------------------------------*/
static void gosub_statement(void){
  int linenum;
  accept(TOKENIZER_GOSUB);
  linenum = tokenizer_num();
  accept(TOKENIZER_NUMBER);
  accept(TOKENIZER_LF);
  if(*gosub_depth_cell < UBASIC_MAX_GOSUB_STACK_DEPTH) {

    /* Push the current return line into the configured gosub stack (memory or internal). */
    gosub_push(tokenizer_num());

    jump_linenum(linenum);
  } else {
    DEBUG_PRINTF("gosub_statement: gosub stack exhausted.\n");
  }
}
/*---------------------------------------------------------------------------*/
static void return_statement(void){
  accept(TOKENIZER_RETURN);
  if(*gosub_depth_cell > 0) {
    int32_t retline = gosub_pop();
    jump_linenum(retline);
  } else {
    DEBUG_PRINTF("return_statement: non-matching return.\n");
  }
}
/*---------------------------------------------------------------------------*/
static void rem_statement(void) {
  accept(TOKENIZER_REM);
  DEBUG_PRINTF("rem_statement: Skipping comment.\n");
  tokenizer_skip();
  if (tokenizer_token() == TOKENIZER_LF) {
    accept(TOKENIZER_LF);
  }
}
/*---------------------------------------------------------------------------*/
static void next_statement(void){
  int var;

  accept(TOKENIZER_NEXT);
  var = tokenizer_variable_num();
  accept(TOKENIZER_VARIABLE);
  if(*for_depth_cell > 0 &&
     var == (int)for_stack[*for_depth_cell - 1].for_variable_index) {
    for_state top = for_stack[*for_depth_cell - 1];
    set_variable(var, get_variable(var) + 1);
    if(get_variable(var) <= top.to) {
      jump_linenum(top.line_after_for);
    } else {
      for_pop();
      accept(TOKENIZER_LF);
    }
  } else {
    if (*for_depth_cell > 0) {
      DEBUG_PRINTF("next_statement: non-matching next (expected %d, found %d).\n",
          (int)for_stack[*for_depth_cell - 1].for_variable_index, var);
    }
    accept(TOKENIZER_LF);
  }

}
/*---------------------------------------------------------------------------*/
static void for_statement(void) {
  int for_variable, to;

  accept(TOKENIZER_FOR);
  for_variable = tokenizer_variable_num();
  accept(TOKENIZER_VARIABLE);
  accept(TOKENIZER_EQ);
  set_variable(for_variable, expr());
  accept(TOKENIZER_TO);
  to = expr();
  accept(TOKENIZER_LF);

  for_state st;
  st.line_after_for = (int32_t)tokenizer_num();
  st.for_variable_index = (int32_t)for_variable;
  st.to = (int32_t)to;
  for_push(st);
  #if VERBOSE
    DEBUG_PRINTF("for_statement: new for, var %d to %d.\n", (int)st.for_variable_index, (int)st.to);
  #endif
  
}
/*---------------------------------------------------------------------------*/
static void peek_statement(void){
  VARIABLE_TYPE peek_addr;
  int var;

  accept(TOKENIZER_PEEK);
  peek_addr = expr();
  accept(TOKENIZER_COMMA);
  var = tokenizer_variable_num();
  accept(TOKENIZER_VARIABLE);
  accept(TOKENIZER_LF);

  set_variable(var, peek_function(peek_addr));
}
/*---------------------------------------------------------------------------*/
static void poke_statement(void)
{
  VARIABLE_TYPE poke_addr;
  VARIABLE_TYPE value;

  accept(TOKENIZER_POKE);
  poke_addr = expr();
  accept(TOKENIZER_COMMA);
  value = expr();
  accept(TOKENIZER_LF);

  poke_function(poke_addr, value);
}
/*---------------------------------------------------------------------------*/
static void end_statement(void)
{
  accept(TOKENIZER_END);
  DEBUG_PRINTF("end_statement: Ending program.\n");
  ended = 1;
}
/*---------------------------------------------------------------------------*/
static void statement(void){
  int token;

  token = tokenizer_token();

  switch(token) {
  case TOKENIZER_PRINT:
    print_statement();
    break;
  case TOKENIZER_IF:
    if_statement();
    break;
  case TOKENIZER_GOTO:
    goto_statement();
    break;
  case TOKENIZER_GOSUB:
    gosub_statement();
    break;
  case TOKENIZER_RETURN:
    return_statement();
    break;
  case TOKENIZER_FOR:
    for_statement();
    break;
  case TOKENIZER_PEEK:
    peek_statement();
    break;
  case TOKENIZER_POKE:
    poke_statement();
    break;
  case TOKENIZER_NEXT:
    next_statement();
    break;
  case TOKENIZER_END:
    end_statement();
    break;
  case TOKENIZER_LET:
    accept(TOKENIZER_LET);
    /* Fall through. */
  case TOKENIZER_VARIABLE:
    let_statement();
    break;
  case TOKENIZER_REM:
    rem_statement();
    break;
  default:
    DEBUG_PRINTF("statement: not implemented %d.\n", token);
    exit(1);
  }
}
/*---------------------------------------------------------------------------*/
static void line_statement(void){
  /* Save position before executing line for resume capability */
  save_position();
  
  #if VERBOSE
    DEBUG_PRINTF("----------- Line number %d ---------\n", tokenizer_num());
  #endif
  index_add(tokenizer_num(), tokenizer_pos());
  accept(TOKENIZER_NUMBER);
  statement();
}
/*---------------------------------------------------------------------------*/
static void set_variable(int varnum, VARIABLE_TYPE value){
  if(variables_mem != NULL && varnum >= 0 && varnum < UBASIC_VARIABLE_COUNT) {
    variables_mem[varnum] = value;
  }
}
/*---------------------------------------------------------------------------*/
static VARIABLE_TYPE get_variable(int varnum){
  if(variables_mem != NULL && varnum >= 0 && varnum < UBASIC_VARIABLE_COUNT) {
    return variables_mem[varnum];
  }
  return 0;
}
/*---------------------------------------------------------------------------*/
static int32_t gosub_pop(void) {
  int32_t ptr = *gosub_depth_cell;
  if (ptr == 0) {
    DEBUG_PRINTF("gosub_pop: underflow (ptr=0)\n");
    return 0;
  }
  ptr--;
  *gosub_depth_cell = ptr;
  return gosub_stack_mem[ptr];
}
/*---------------------------------------------------------------------------*/
static void gosub_push(int32_t line_num) {
  int32_t ptr = *gosub_depth_cell;
  if (ptr >= UBASIC_MAX_GOSUB_STACK_DEPTH) {
    DEBUG_PRINTF("gosub_push: overflow (ptr=%d)\n", (int)ptr);
    return;
  }
  gosub_stack_mem[ptr] = line_num;
  *gosub_depth_cell = ptr + 1;
}
/*---------------------------------------------------------------------------*/
static void for_push(for_state state) {
  int32_t depth = *for_depth_cell;
  if (depth < UBASIC_MAX_FOR_STACK_DEPTH) {
    for_stack[depth++] = state;
    *for_depth_cell = depth;
  } else {
    DEBUG_PRINTF("for_push: for stack depth exceeded.\n");
  }
}
/*---------------------------------------------------------------------------*/
static for_state for_pop(void) {
  for_state error_state = {-1, '\0', 0};
  int32_t depth = *for_depth_cell;
  if (depth > 0) {
    for_state value = for_stack[--depth];
    *for_depth_cell = depth;
    return value;
  } else {
    DEBUG_PRINTF("for_pop: for stack underflow.\n");
    return error_state;
  }
}
/*---------------------------------------------------------------------------*/



