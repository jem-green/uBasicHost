/*
 * Example: Automatic saving and resuming execution of a uBASIC program
 * 
 * This demonstrates how to:
 * 1. Run a program for a few steps (position saved automatically)
 * 2. Save the entire memory block (simulated here)
 * 3. Reload memory and resume from saved position
 * 
 * Note: Position is saved automatically before each line executes.
 * You only need to call ubasic_resume() after reloading memory.
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "ubasic.h"

#define MEMORY_SIZE 4096

const char *test_program = 
    "10 LET A = 1\n"
    "20 PRINT A\n"
    "30 LET A = A + 1\n"
    "40 IF A < 10 THEN GOTO 20\n"
    "50 PRINT \"Done\"\n"
    "60 END\n";

void print_callback(const char *value) {
    printf("%s", value);
}

int main() {
    uint8_t *memory = malloc(MEMORY_SIZE);
    uint8_t *saved_memory = malloc(MEMORY_SIZE);
    
    if (!memory || !saved_memory) {
        printf("Failed to allocate memory\n");
        return 1;
    }
    
    /* Initialize uBASIC */
    ubasic_init(memory, MEMORY_SIZE);
    ubasic_callback(print_callback);
    ubasic_load_program(test_program);
    
    printf("=== Running first 5 iterations ===\n");
    
    /* Run for 5 iterations - position is saved automatically before each line */
    for (int i = 0; i < 5 && !ubasic_finished(); i++) {
        ubasic_run();
    }
    
    /* Get the automatically saved position (stored at memory offset 0) */
    int32_t saved_offset = *(int32_t*)(memory + 0);
    printf("\n=== Automatically saved at offset: %d ===\n", saved_offset);
    
    /* Simulate saving memory to disk/storage */
    memcpy(saved_memory, memory, MEMORY_SIZE);
    printf("=== Memory saved ===\n\n");
    
    /* Continue running (optional - just to show it works) */
    printf("=== Continuing original execution ===\n");
    for (int i = 0; i < 5 && !ubasic_finished(); i++) {
        ubasic_run();
    }
    
    printf("\n=== Simulating program restart ===\n");
    
    /* Simulate reloading from disk */
    memcpy(memory, saved_memory, MEMORY_SIZE);
    
    /* Re-initialize pointers (as if we just loaded memory) - MUST use same size! */
    ubasic_init(memory, MEMORY_SIZE);
    ubasic_callback(print_callback);
    
    /* Resume from saved position */
    printf("=== Resuming from offset: %d ===\n", *(int32_t*)(memory + 0));
    ubasic_resume();
    
    /* Continue execution from saved point */
    printf("=== Running after resume ===\n");
    while (!ubasic_finished()) {
        ubasic_run();
    }
    
    printf("\n=== Program completed ===\n");
    
    free(memory);
    free(saved_memory);
    
    return 0;
}
