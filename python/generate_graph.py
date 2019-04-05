import argparse

parser = argparse.ArgumentParser(description='Generate random graph.')
parser.add_argument('nodes', metavar='N', type=int, nargs='+',
                   help='Number of nodes in the graph')
parser.add_argument('--sum', dest='accumulate', action='store_const',
                   const=sum, default=max,
                   help='sum the integers (default: find the max)')

args = parser.parse_args()

MAX_WEIGHT = 10

def graphGenerate(numNodes, directed):
    graph = []
    for i in range(1, numNodes):
        for j in range(1, numNodes):
            edgeWeight = random.randrange(0,MAX_WEIGHT))
            if directed:
                # i -> j does not necessarily mean j -> i
                graph[i][j] = edgeWeight 
            else:
                graph[i][j] = edgeWeight
                graph[j][i] = edgeWeight
    

