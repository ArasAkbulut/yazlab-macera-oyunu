import sys
from pathlib import Path

import matplotlib.pyplot as plt
from tensorboard.backend.event_processing import event_accumulator

run_dir = Path(sys.argv[1]) if len(sys.argv) > 1 else Path("results/companion-v6/Companion")
out_path = Path(sys.argv[2]) if len(sys.argv) > 2 else Path("results/companion-v6/reward_graph.png")

ea = event_accumulator.EventAccumulator(str(run_dir))
ea.Reload()

tag = next(t for t in ea.Tags()["scalars"] if "Cumulative Reward" in t)
events = ea.Scalars(tag)
steps = [e.step for e in events]
values = [e.value for e in events]

plt.figure(figsize=(8, 4.5))
plt.plot(steps, values, marker="o", color="#2E86AB")
plt.axhline(0, color="gray", linewidth=0.8, linestyle="--")
plt.title("Companion AI - Eğitim Boyunca Ortalama Ödül")
plt.xlabel("Adım (Step)")
plt.ylabel("Mean Cumulative Reward")
plt.grid(alpha=0.3)
plt.tight_layout()
plt.savefig(out_path, dpi=150)
print(f"Saved: {out_path}")
