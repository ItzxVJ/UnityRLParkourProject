# Unity ML Parkour Agents

A Unity-based reinforcement learning project where autonomous agents learn to navigate and complete parkour environments.

The project uses **Unity ML-Agents** to train agents through reinforcement learning rather than manually programming their movement. The agents learn how to move through different obstacles and adapt their behavior based on the environment.

## Overview

The main goal of the project is to experiment with reinforcement learning for autonomous movement and navigation.

The training environment includes different parkour obstacles that require the agent to learn movement strategies through repeated interaction with the environment.

## Features

* Reinforcement learning with Unity ML-Agents
* Autonomous parkour agents
* Dynamic training environments
* Curriculum-based training
* Parallelized training environments
* Agents learning movement through trial and error
* Configurable training parameters

## Training

Agents are trained using reinforcement learning, receiving feedback based on their actions and progress through the environment.

Training uses multiple simulation instances running concurrently, allowing the agent to collect more experience without having to run each environment sequentially.

Parallelizing the environments improved training efficiency by approximately **4×** compared to the original setup.

## Curriculum Learning

The project uses curriculum-based training to gradually increase the difficulty of the parkour environment.

Instead of immediately placing the agent in the most difficult environment, training can progress through different levels of difficulty as the agent becomes more capable.

This makes it possible to experiment with how different environments and difficulty levels affect the agent's ability to learn.

## Technology

* **Unity**
* **Unity ML-Agents**
* **C#**
* Reinforcement Learning
* Curriculum Learning

## Project Structure

The project contains the Unity environment, agent logic, training configurations, and training results used throughout development.

Training configurations and experiment results are included in the repository to make it easier to reproduce and compare different training runs.

## Results

The project was developed through multiple training experiments, with improvements made to the training setup and environment throughout development.

Parallelizing the simulation environments reduced training time by approximately **4×**, allowing more training iterations to be run within the same amount of time.

## Purpose

This project was built to explore reinforcement learning in a simulated environment and gain practical experience with training autonomous agents, designing reward systems, and improving training efficiency.
