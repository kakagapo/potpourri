package potpourri;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.ObjectInputStream;
import java.io.ObjectOutputStream;
import java.io.Serializable;

public class Serialization {
	enum MessageType{
		Ping,
		Pong;
	}
	
	static class Message implements Serializable{

		/**
		 * Eclipse generated serialization version.
		 * Used for versioning purposes and also to fail fast when the versions don't match  
		 */
		private static final long serialVersionUID = 4985214177098275556L;
		
		int version;
		MessageType messageType;
		String payload;
		
		Message(int version, MessageType messageType, String payload){
			this.version = version;
			this.messageType = messageType;
			this.payload = payload;
		}
		
		@Override
		public boolean equals(Object o) {
			if (o == this) {
	            return true;
	        }
	 
	        
	        if (!(o instanceof Message)) {
	            return false;
	        }
	        
	        Message otherMsg = (Message)o;
	        return version == otherMsg.version && messageType == otherMsg.messageType && payload.equals(otherMsg.payload);	         
		}
	}
	
	public static void main(String[] args) throws Exception{
		
		File tempFile = File.createTempFile("temp-file-name", ".tmp");
		FileOutputStream fos = new FileOutputStream(tempFile);
		ObjectOutputStream out = new ObjectOutputStream(fos);
         
		Message msg = new Message(1, MessageType.Ping, "hi!");        
        
		
		//this where you do the actual write
		out.writeObject(msg);         
        
		out.close();
        fos.close();
        
        FileInputStream fis = new FileInputStream(tempFile);
        ObjectInputStream in = new ObjectInputStream(fis);
        Message msgFromFile = (Message)in.readObject();
        
        in.close();
        fis.close();
        
        if(msg.equals(msgFromFile)) {
        	System.out.println("Worked");
        }
	}

}
