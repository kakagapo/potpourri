# dump of code seen in Youtube tutorial

import pandas_datareader.data as web
import pandas as pd
import datetime as dt
import numpy as np
import matplotlib.pyplot as plt
from matplotlib import style

style.use('ggplot')

prices = web.DataReader('F', 'robinhood')
prices.head()


start = dt.datetime(2019, 1, 1)
end = dt.datetime(2019, 1, 5)

# data reader does not seem to be working
prices = web.DataReader('AAPL', 'robinhood', start, end)['close_price']

returns = prices.pct_change()

last_price = prices[-1]

num_simulations = 1000
num_days = 250

simulation_df = pd.DataFrame()

for x in range(num_simulations):
    count = 0
    daily_vol = returns.std()
    price_series = []
    
    price = last_price * (1 + np.random.normal(0, daily_vol))
    price_series.append(price)
    
    for y in range(num_days-1):
        price = price_series[-1] * (1 + np.random.normal(0, daily_vol))
        price_series.append(price)
        count += 1
    
    simulation_df[x] = price_series
    
fig = plt.figure()
fig.suptitle('Monte Carlo Simulation: AAPL')
plt.plot(simulation_df)
plt.axhline(y = last_price, color = 'r', linestyle = '-')
plt.xlabel('Day')
plt.ylabel('Price')
plt.show()
