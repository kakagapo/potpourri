import urllib.request
import urllib.error
import argparse

parser = argparse.ArgumentParser(description='Makes a HTTP GET call.')
parser.add_argument('url')
parser.add_argument('--verbose', action='store_true')

args = parser.parse_args()

url = args.url
verbose = args.verbose

try:
    with urllib.request.urlopen(url) as fp:
        if verbose:
            print("Response headers:\r\n")
            print(fp.getheaders())
            print("\r\n")
        for line in fp:
            print(line)
except urllib.error.HTTPError as e:
    print("Code ", e.code, ", ", e.reason)

