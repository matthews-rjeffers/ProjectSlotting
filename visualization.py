import numpy as np
import matplotlib.pyplot as plt
from matplotlib.animation import FuncAnimation

# Create figure and axis
fig, ax = plt.subplots(figsize=(10, 10), facecolor='black')
ax.set_facecolor('black')
ax.set_xlim(-15, 15)
ax.set_ylim(-15, 15)
ax.set_aspect('equal')
ax.axis('off')

# Initialize spiral galaxy parameters
n_arms = 3
n_particles = 2000
particles_per_arm = n_particles // n_arms

# Generate spiral galaxy particles
def generate_galaxy(frame):
    particles = []
    colors = []
    sizes = []

    for arm in range(n_arms):
        # Angular offset for each arm
        angle_offset = arm * (2 * np.pi / n_arms)

        # Generate particles along spiral arm
        t = np.linspace(0, 4 * np.pi, particles_per_arm)

        # Spiral equation with time-based rotation
        theta = t + angle_offset + frame * 0.02
        r = t * 0.8

        # Convert to Cartesian coordinates
        x = r * np.cos(theta)
        y = r * np.sin(theta)

        # Add some randomness for realistic effect
        x += np.random.normal(0, 0.3, particles_per_arm)
        y += np.random.normal(0, 0.3, particles_per_arm)

        particles.extend(zip(x, y))

        # Color gradient from center to edge
        color_values = t / (4 * np.pi)
        arm_colors = plt.cm.hsv(np.linspace(arm/n_arms, arm/n_arms + 0.3, particles_per_arm))
        colors.extend(arm_colors)

        # Size variation - smaller at edges
        sizes.extend(50 * (1 - color_values/2))

    # Add central bulge
    n_bulge = 200
    bulge_r = np.random.exponential(0.5, n_bulge)
    bulge_theta = np.random.uniform(0, 2*np.pi, n_bulge)
    bulge_x = bulge_r * np.cos(bulge_theta)
    bulge_y = bulge_r * np.sin(bulge_theta)

    particles.extend(zip(bulge_x, bulge_y))
    colors.extend([plt.cm.YlOrRd(0.9)] * n_bulge)
    sizes.extend([100] * n_bulge)

    return particles, colors, sizes

# Animation function
scatter = None

def animate(frame):
    global scatter
    ax.clear()
    ax.set_facecolor('black')
    ax.set_xlim(-15, 15)
    ax.set_ylim(-15, 15)
    ax.set_aspect('equal')
    ax.axis('off')

    particles, colors, sizes = generate_galaxy(frame)
    x_vals, y_vals = zip(*particles)

    scatter = ax.scatter(x_vals, y_vals, c=colors, s=sizes, alpha=0.6)
    ax.set_title('Spiral Galaxy Visualization', color='white', fontsize=16, pad=20)

    return scatter,

# Create animation
anim = FuncAnimation(fig, animate, frames=200, interval=50, blit=True)

print("🌌 Creating your spiral galaxy visualization...")
print("✨ Close the window to exit")
plt.tight_layout()
plt.show()
