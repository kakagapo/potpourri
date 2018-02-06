package potpourri;

import java.io.File;
import java.io.RandomAccessFile;
import java.nio.MappedByteBuffer;
import java.nio.channels.FileChannel;
 
public class MemoryMapping {
    private static String FILE = "temp.txt";
 
    public static void main(String[] args) throws Exception
    {
    	//write();
    	read();
    }
    
    public static void write() throws Exception{
    	
        File file = new File(FILE);
        file.delete();
        FileChannel fileChannel = new RandomAccessFile(file, "rw").getChannel();
        // Get direct byte buffer access using channel.map() operation
        MappedByteBuffer buffer = fileChannel.map(FileChannel.MapMode.READ_WRITE, 0, 4096 * 8 * 8);        
        buffer.put("howtodoinjava.com".getBytes());
    }
    
    public static void read() throws Exception{
        File file = new File(FILE);
        FileChannel fileChannel = new RandomAccessFile(file, "r").getChannel();        
        MappedByteBuffer buffer = fileChannel.map(FileChannel.MapMode.READ_ONLY, 0, fileChannel.size());
        System.out.println("Is buffer loaded :" + buffer.isLoaded());  //prints false
        System.out.println("Buffer capacity :" + buffer.capacity() + ", limit :" + buffer.limit());
        
        for (int i = 0; i < buffer.limit(); i++)
        {
            //System.out.print((char) buffer.get()); //Print the content of file
        }
        System.out.println("Is buffer loaded :" + buffer.isLoaded());
    }
}
