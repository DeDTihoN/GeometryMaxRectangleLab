import os
import sys
import json
from asyncio import sleep

import cvxpy as cp

if __name__ == "__main__":

    script_dir = os.path.dirname(os.path.abspath(__file__))

    file_path = os.path.join(script_dir, 'data.json')
    with open(file_path, 'r') as f:
        data = json.load(f)
    P1 = data['P1']
    P2 = data['P2']
    B = data['B']
    t = data['t']

    u1 = cp.Variable()
    u2 = cp.Variable()
    v1 = cp.Variable()
    v2 = cp.Variable()
    x1 = cp.Variable()
    x2 = cp.Variable()
    y1 = cp.Variable()
    y2 = cp.Variable()
    z1 = cp.Variable()
    z2 = cp.Variable()

    objective = cp.Minimize(-cp.log(u1) - cp.log(v2))
    constraints = [
        u2 - t * u1 == 0,
        v1 + t * v2 == 0,
        u1 - y1 + x1 == 0,
        u2 - y2 + x2 == 0,
        v1 - z1 + x1 == 0,
        v2 - z2 + x2 == 0
    ]

    for i in range(len(B)):
        p1 = P1[i]
        p2 = P2[i]
        b = B[i]
        constraints += [p1 * x1 + p2 * x2 <= b]
        constraints += [p1 * y1 + p2 * y2 <= b]
        constraints += [p1 * z1 + p2 * z2 <= b]
        constraints += [p1 * (y1 + z1 - x1) + p2 * (y2 + z2 - x2) <= b]

    prob = cp.Problem(objective, constraints)
    prob.solve()

    result_list = [x1.value.item(), x2.value.item(), y1.value.item(), y2.value.item(), z1.value.item(), z2.value.item()]

    print(json.dumps(result_list))