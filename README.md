# FiveM-Vehicle-Behavior
Inverse Torque and Custom Wheelies packed neatly into a single script. In the future this will help other vehicle-affecting features cooperate.


## Server Convars
Define these in server.cfg so the script can work properly.


```
## Vehicle Behavior (Inverse Torque, Custom Wheelies)
# Inverse Torque
ensure vehiclebehavior
setr it_on 1
setr it_power_scale_percent 200
setr it_grip_scale_percent 100
setr it_awd_penalty_percent 25

# Custom Wheelies
setr cw_disable_vanilla 1
setr cw_enable_custom_wheelies 1
setr cw_power_scale_percent 100
```

### Inverse Torque
it_on [1|0] enables or disables Inverse Torque entirely.

it_power_scale_percent [X00] scales the torque multiplier up and down based on the vehicle's power. 

it_grip_scale_percent [X00] scales the torque multiplier up and down based on the vehicle's grip.

it_awd_penalty_percent [XX] scales the torque multiplier down based on the vehicle's AWD bias.
0/100 (RWD) doesn't get affected, 50/50 has the torque multiplier reduced by this percent. 25 = 75% of the final multiplier.

### Custom Wheelies
cw_disable_vanilla [1|0] keeps vanilla wheelies (for muscles and other specific veihcles) disabled.

cw_enable_custom_wheelies [1|0] enables custom wheelie behavior for all vehicles. No filter yet.

cw_power_scale_percent [X00] scales up and down how powerful the wheelie is based on the vehicle's power.
