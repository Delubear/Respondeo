Feature: Pray a devotion
  As a visitor who wants to pray
  I want to open a devotion and pray it bead by bead
  So that a guide keeps my place through each prayer

  Scenario: The devotions browser lists devotions to pray
	Given I open the devotions browser
	Then I should see at least one devotion card

  Scenario: Opening a devotion shows its intro and a way to begin
	Given I open the devotions browser
	When I choose the first devotion
	Then the devotion intro should be visible
	And I should see a way to begin praying

  Scenario: Beginning a devotion enters the praying guide
	Given I open the devotions browser
	When I choose the first devotion
	And I begin praying
	Then the praying guide should be visible
	And I should see a way to leave the devotion
