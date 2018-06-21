local removedKeys = 0
for i=1,#KEYS-1 do
	removedKeys = removedKeys + redis.call('del', KEYS[i])
end
return removedKeys
