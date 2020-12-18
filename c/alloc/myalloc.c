#include "myalloc.h"
#include <stdbool.h>

struct chunk
{
    size_t chunk_size;
    bool allocated; // a boolean value indicating whether or not a chunk is allocated
    chunk_t* next_chunk;
    chunk_t* prev_chunk;
    // Immediately following these fields is the payload
};

typedef struct chunk chunk_t;

// This will come in handly when you have to figure out the start of the chunk header given
// a pointer to chunk payload during free.
const int CHUNK_HEADER_SIZE = sizeof(chunk_t);

struct free_list
{
    chunk_t* start;
    // Todo: add more augmenting info to help with fast allocations
};

typedef struct free_list free_list_t;
static free_list_t* free_chunk_list;


// Why do we need to maintain a allocated_list???
struct allocated_list
{
    chunk_t* start;
};

typedef struct allocated_list allocated_list_t;
static allocated_list_t allocated_list_instace;

void* allocate_memory(size_t size)
{
    if(free_chunk_list != NULL)
    {
        // Go through free list and return a free chunk
        chunk_t* current_chunk = free_chunk_list->start;
        while(current_chunk != NULL)
        {
            assert(!current_chunk->allocated);

            if(current_chunk->chunk_size > size)
            {
                size_t required_size = size + CHUNK_HEADER_SIZE;
                if(current_chunk->chunk_size > required_size)
                {
                    // We have enough extra space to split
                    
                }                
            }
            else if (current_chunk->chunk_size == size)
            {
                // exact match, so just remove the chunk from the free list
                
            }
            current_chunk = current_chunk->next_chunk;
        }

        // todo: no existing chunk in free list matches so get from OS.
    }
    else
    {
        // todo: initilize free list and load from OS

    }
}

void my_free(void* ptr)
{
    // Todo: return the chunk to free list and 
    // If possible merge adjacent free chunks together
    
    // todo: validations

}