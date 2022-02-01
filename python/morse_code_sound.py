
import sys
import os
import string
import winsound
import time

class MorseCodeBeeper:
    DOT_DURATION_MILLI_SEC = 100
    BEEP_FREQ = 800 # In Hz

    code_table = {
        "A":    ". _",
        "B":    "_ . . .",
        "C":    "_ . _ .",
        "D":    "_ . .",
        "E":    ".",
        "F":    ". . _ .",
        "G":    "_ _ .",
        "H":    ". . . .",
        "I":    ". .",
        "J":    ". _ _ _",
        "K":    "_ . _",
        "L":    ". _ . .",
        "M":    "_ _",
        "N":    "_ .",
        "O":    "_ _ _",
        "P":    ". _ _ .",
        "Q":    "_ _ . _",
        "R":    ". _ .",
        "S":    ". . .",
        "T":    "_",
        "U":    ". . _",
        "V":    ". . . _",
        "W":    ". _ _",
        "X":    "_ . . _",
        "Y":    "_ . _ _",
        "Z":    "_ _ . .",
        "0":	"_",
        "1":	". _",	 	
        "2":	". . _", 	
        "3":	". . . _",
        "4":	". . . . _",	 	
        "5":	".",
        "6":	"_ . . . .",
        "7":	"_ . . .",
        "8":	"_ . .",
        "9":	"_ ." 	 
    }

    def encode(self, text):
        # add validation to confirm that the text has only english alphabets and digits
        if not all(x.isalnum() or x.isspace() for x in text):
            raise SystemExit("Only alphabetical letters and spaces: no")
        
        text = text.upper()

        output = ""
        space_previous = False
        for c in text:
            if c.isalnum():
                code =  self.code_table[c].replace(" ", "")
                output += code
            else:
                if not space_previous:
                    # this has to be space as we have enforced in validation
                    output += " "
                    space_previous = True
                else:
                    pass
        return output

    def make_sound(self, text):
        encoded_text = self.encode(text)
        for c in encoded_text:
            if c == ".":
                self.make_sound_dot()
                self.letter_space_silence()
            elif c == "_":
                self.make_sound_dash()
                self.letter_space_silence()
            else:
                self.word_space_silence()
    
    def make_sound_dot(self):
        winsound.Beep(self.BEEP_FREQ, self.DOT_DURATION_MILLI_SEC)
    
    def make_sound_dash(self):
        winsound.Beep(self.BEEP_FREQ, self.DOT_DURATION_MILLI_SEC * 3)
    
    def letter_space_silence(self):
        time.sleep(self.DOT_DURATION_MILLI_SEC/1000)
    
    def letter_space_silence(self):
        time.sleep((self.DOT_DURATION_MILLI_SEC/1000)*3)

            
m = MorseCodeBeeper()
print(m.encode("sos   sos"))

m.make_sound("sos")