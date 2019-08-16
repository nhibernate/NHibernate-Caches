local values = {}
for i=1,#KEYS do
	values[i] = redis.call('pttl', KEYS[i])
end
return values
