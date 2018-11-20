// To build: g++ pthread.cpp -lpthread
#include <pthread.h>
#include <iostream>

using namespace std;

void* thread_operation(void* param){
    for(int i=0; i<5; i++){
        count << "Hi number " << i << " from thread " << pthread_self();
    }
    pthread_exit();
}

int main(){
    pthread_t t1, t2;

    pthread_create(&t1, NULL, thread_operation, NULL /* args to pass to thread start routine */);
    pthread_create(&t1, NULL, thread_operation, NULL );

    pthread_join(t1, NULL);
    pthread_join(t2, NULL);
    return 0;
}