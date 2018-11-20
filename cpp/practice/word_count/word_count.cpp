// To build : g++ word_count.cpp
// To run : cat some_file.txt | a.out
#include <stdlib.h>
#include <stdio.h>
#include <cctype.h>

int main(int argc, char** argv)
{
    unsigned int num_chars = 0;
    unsigned int num_words = 0;
    unsigned int num_lines = 0;
    bool read_from_stdin = false;
    FILE* f;

    char ch = NULL, prev_ch = NULL;
    if(argc == 1)
    {
        read_from_stdin = true;
    }
    else if(argc == 2)
    {
        f = fopen(argv[1], "r");
        if(f == NULL) perror("Invalid input file.");
    }
    else
    {
        perror("Incorrect number of args.");
    }
    while((ch = read_from_stdin ? getchar() : getc(f)) != EOF)
    {
        num_chars++;
        if(ch == '\n')
        {
            num_lines++;
            if(!isspace(prev_ch)){
                num_words++;
            }
        }
        else if(isspace(ch))
        {
            if(!isspace(prev_ch)){
                num_words++;
            }
        }
        else
        {
            num_chars++;
        }
        prev_ch = ch;
    } // end while

    if(!isspace(prev_ch) && prev_c != NULL)
    {
        num_words++;
    }

    cout << "characters = " << num_chars << ", words = " << num_words << ", lines = " num_lines;
    return 0;

}