#!/usr/bin/env python

import sys, getopt

from PIL import Image
from PIL.ExifTags import TAGS
from PIL.ExifTags import GPSTAGS

def get_exif(filename):
    image = Image.open(filename)
    image.verify()
    return image._getexif()

def get_labeled_exif(exif):
    labeled = {}
    for (key, val) in exif.items():
        labeled[TAGS.get(key)] = val
    return labeled

def get_geotagging(exif):
    if not exif:
        raise ValueError("No EXIF metadata found")

    geotagging = {}
    for (idx, tag) in TAGS.items():
        if tag == 'GPSInfo':
            if idx not in exif:
                raise ValueError("No EXIF geotagging found")

            for (key, val) in GPSTAGS.items():
                if key in exif[idx]:
                    geotagging[val] = exif[idx][key]

    return geotagging

def strip_geotagging(file):
    # todo
    pass

def strip_all_exif_metadata(fileIn, fileOut):
    # todo
    image = Image.open(fileIn)

    # next 3 lines strip exif
    data = list(image.getdata())
    image_without_exif = Image.new(image.mode, image.size)
    image_without_exif.putdata(data)
    image_without_exif.save('image_file_without_exif.jpeg')


def add_layer_and_show(backgroundImage, foregroundImage):
    backgroundImage.paste(foregroundImage, (0, 0), foregroundImage)
    backgroundImage.show()

def main(argv):
    inputfile = ''
    outputfile = ''
    command = ''
    try:
        opts, args = getopt.getopt(argv,"hi:o:",["ifile=","ofile="])
    except getopt.GetoptError:
        print('Image_misc.py -c <command> -i <inputfile> -o <outputfile>')
        sys.exit(2)

    for opt, arg in opts:
        if opt == '-h':
            print('Image_misc.py -i <inputfile> -o <outputfile>')
            sys.exit()
        elif opt in ("-c", "--command"):
            command = arg
        elif opt in ("-i", "--input-file"):
            inputfile = arg
        elif opt in ("-o", "--output-file"):
            outputfile = arg

    print('Input file is "', inputfile)
    print('Output file is "', outputfile)

    if(command == "dump-metadata"):
        exif = get_exif(inputfile)
        labeled = get_labeled_exif(exif)
        print(labeled)
    elif(command == "dump-geo-metadata"):
        exif = get_exif(inputfile)
        geo_info = get_geotagging(exif)
        print(geo_info)
    elif(command == "strip-metadata"):
        if(not outputfile):
            outputfile = "copy_" + inputfile
        strip_all_exif_metadata(inputfile, outputfile)