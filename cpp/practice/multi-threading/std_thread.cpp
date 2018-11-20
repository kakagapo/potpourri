// To build: g++ std_thread.cpp -std=c++17 -lpthread
#include <thread>
#include <iostream>

using namespace std;

void* thread_operation(){
    for(int i=0; i<5; i++){
        // std::this_thread is a namespace and is used to group functions accessing current thread.
        cout << "Hi number " << i << " from thread " << this_thread::get_id() << endl;
    }
}

int main(){
    thread t1 (thread_operation), t2 (thread_operation);

    t1.join();
    t2.join();
    
    return 0;
}