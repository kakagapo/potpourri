import csv
from pprint import pprint

def filterLowCounts(data):
	# only when count >= 500
	return {k: v for k,v in data.items() if v >= 500}

def getData(filePath):
	data={}
	with open(filePath, 'r') as f:
		reader = csv.reader(f)
		for row in reader:
			#pprint(row)
			if row[0] in data:
				data[row[0]] += int(row[2])
			else:
				data[row[0]] = int(row[2])
	#pprint(data)
	return data

new_data = filterLowCounts(getData("new.csv"))
base_data = filterLowCounts(getData("base.csv"))

print("New occurrence:")
pprint(set(new_data.keys()) - set(base_data.keys()))

print("With large changes (more than 50% increase):")
for k,v in new_data.items():
	if k in base_data:
		if v > base_data[k]:
			increasePercent = ((v - base_data[k]) / base_data[k]) * 100.0
			if increasePercent >  50 :
				print(k, ", new val:", v, ", base val: ", base_data[k], ", increase percent : ", increasePercent)
