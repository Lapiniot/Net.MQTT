﻿namespace Net.Mqtt;

public abstract class MqttPacketWithId
{
    protected MqttPacketWithId(ushort id)
    {
        if (id == 0)
        {
            ThrowHelper.ThrowInvalidPacketId(id);
        }

        Id = id;
    }

    public ushort Id { get; }
}