# from rosetta code
# this seems to print stuff in color

import matplotlib.pyplot as plt
import numpy as np

npts = 300
max_iter = 100

# X and Y are of type numpy.ndarray and are of len 600 and 300 resp.
X = np.linspace(-2, 1, 2 * npts)
Y = np.linspace(-1, 1, npts)

print(f'type(X):{type(X)}, shape;{X.shape}, len(X):{len(X)}')
print(f'type(Y):{type(Y)}, shape;{Y.shape}, len(Y): {len(Y)}')

#broadcast X to a square array
# J or lower case j can be use to represent complex number
C = X[:, None] + 1J * Y 

# In case of ndarray, len just returns the length of the 1st dimension
print(f'type(C):{type(C)}, len(C):{len(C)}, C.shape:{C.shape}, C.size:{C.size}')

#initial value is always zero
Z = np.zeros_like(C)

exit_times = max_iter * np.ones(C.shape, np.int32)
mask = exit_times > 0

for k in range(max_iter):
    Z[mask] = Z[mask] * Z[mask] + C[mask]
    mask, old_mask = abs(Z) < 2, mask
    #use XOR to detect the area which has changed 
    exit_times[mask ^ old_mask] = k

plt.imshow(exit_times.T,
           cmap=plt.cm.prism,
           extent=(X.min(), X.max(), Y.min(), Y.max()))
plt.show()