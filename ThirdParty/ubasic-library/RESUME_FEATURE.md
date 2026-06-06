# uBASIC Resume Feature

## Overview

The resume feature automatically saves the execution position before each line is executed, allowing you to save the entire memory state and resume from the last executed line later. This is useful for:
- Implementing save/restore functionality
- Checkpointing long-running programs
- Pausing and resuming execution across program restarts
- Creating persistent program state

## How It Works

**Automatic Position Saving**: Every time a line is about to execute (in `line_statement`), the current position is automatically saved to memory offset 0. You don't need to call any save function - it happens automatically.

**Manual Resume**: After reloading saved memory, call `ubasic_resume()` to restore the execution position.

## Memory Layout

The resume offset is stored at memory position 0 (offset 0) as an `int32_t` value. This contains the byte offset from the start of the program text.

Memory layout:
```
Offset 0:  resume_offset (int32_t) - automatically saved execution position
Offset 4:  gosub_depth (int32_t) - GOSUB stack depth
Offset 8:  for_depth (int32_t) - FOR loop stack depth
Offset 12: gosub_stack[] - GOSUB return addresses
...
```

## API Functions

### `void ubasic_resume(void)`
Resumes execution from the position stored at memory offset 0.

**Usage:**
```c
ubasic_resume();  // Resume from saved position
```

### `int32_t ubasic_get_resume_offset(void)`
Returns the saved resume offset value (for debugging/informational purposes).

**Returns:** The byte offset from program start, or 0 if not set.

**Usage:**
```c
int32_t offset = ubasic_get_resume_offset();
printf("Resume offset: %d\n", offset);
```

## Usage Pattern

### Basic Save/Resume (Automatic Position Saving)
```c
uint8_t memory[4096];

// 1. Initialize and load program
ubasic_init_sized(memory, sizeof(memory));
ubasic_load_program(program_text);

// 2. Run for a while - position is saved automatically before each line
for (int i = 0; i < 10; i++) {
    ubasic_run();  // Position saved automatically in line_statement
}

// 3. Save entire memory to disk/storage
save_to_file("save.dat", memory, sizeof(memory));

// ... Later, or in a different program run ...

// 4. Load memory from disk/storage
load_from_file("save.dat", memory, sizeof(memory));

// 5. Re-initialize (resets internal pointers)
ubasic_init_sized(memory, sizeof(memory));

// 6. Resume execution from saved position
ubasic_resume();

// 7. Continue running - will resume from last line executed
while (!ubasic_finished()) {
    ubasic_run();
}
```

### Checking if Resume Data Exists
```c
int32_t offset = ubasic_get_resume_offset();
if (offset > 0) {
    printf("Resume data found, resuming from offset %d\n", offset);
    ubasic_resume();
} else {
    printf("No resume data, starting from beginning\n");
}
```

## Important Notes

1. **Complete Memory Save**: When saving, you must save the entire memory block, not just the resume offset. The memory contains:
   - Resume offset
   - Control flow state (GOSUB and FOR stacks)
   - Automatic Position Saving**: The execution position is automatically saved before each line executes. You don't need to call any save function.

2. **Complete Memory Save**: When saving, you must save the entire memory block, not just the resume offset. The memory contains:
   - Resume offset (automatically updated)
   - Control flow state (GOSUB and FOR stacks)
   - Program text
   - Variable values

3. **Re-initialization Required**: After loading memory, you must call `ubasic_init_sized()` again to re-establish internal pointers before calling `ubasic_resume()`.

4. **Preservation of State**: The resume feature preserves:
   - Current execution position (saved before each line)
   - Variable values (A-Z)
   - GOSUB return stack
   - FOR loop state
   - Program text

5. **Line-Level Granularity**: Position is saved at line boundaries. When you resume, the program will re-execute the line it was on when the memory was saved (or continue to the next line if the previous line completed)
See [resume_example.c](resume_example.c) for a complete working example.

## Technical Detailsautomatically saved at the start of `line_statement()` before each line executes
- The offset is stored as a byte offset from `tokenizer_start()` to `tokenizer_pos()`
- When resuming, `tokenizer_goto()` is called to restore the tokenizer position
- The `ended` flag is cleared on resume to allow continued execution
- All BASIC control flow state (GOSUB/FOR stacks) is preserved in memory
- No manual intervention needed - just run your program and save memory whenever you wantosition
- The `ended` flag is cleared on resume to allow continued execution
- All BASIC control flow state (GOSUB/FOR stacks) is preserved in memory
